#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Wafer Defect — Segmentation or Classification (PyTorch + SMP)
-------------------------------------------------------------
- Segmentation mode ("seg"): background / wafer / defect(또는 per-category) 학습
- Classification mode ("cls"): 이미지 전체를 OK/카테고리 분류 (메타의 category 이용, 없으면 DEFECT/OK로 이진 분류)

Assumed dataset layout (당신 툴과 호환):
ROOT/
  dataset/
    raw/  *.png|jpg   (원본)
    seg/  *.png       (마스크: 정수 라벨)
    meta/ *.json      (옵션: {"category": "SCRATCH", ...})
    viz/  (출력용; 없어도 무관)

Usage (Segmentation):
  python wafer_train.py seg --data ROOT/dataset --epochs 50 --img-size 768 \
      --arch deeplabv3plus --encoder resnet50 --three-classes --wafer-id 1

Usage (Classification fallback):
  python wafer_train.py cls --data ROOT/dataset --epochs 20 --img-size 512 \
      --backbone tf_efficientnet_b0 --min-defect-px 64
"""

import os, json, math, random, argparse, time, sys
from glob import glob
from typing import List, Tuple, Dict, Optional

import cv2
import numpy as np
from PIL import Image

import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import Dataset, DataLoader, WeightedRandomSampler, Subset, default_collate

import albumentations as A
from albumentations.pytorch import ToTensorV2

try:
    import segmentation_models_pytorch as smp
except Exception:
    smp = None
import timm
# Non-interactive plots for reports
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
# Ensure line-buffered stdout/stderr for real-time log streaming when piped
try:
    sys.stdout.reconfigure(line_buffering=True)
    sys.stderr.reconfigure(line_buffering=True)
except Exception:
    pass
try:
    from augmentations import build_cls_transforms  # type: ignore
except Exception:
    build_cls_transforms = None  # type: ignore


# -----------------------
# Utils
# -----------------------
def set_seed(seed=42):
    random.seed(seed); np.random.seed(seed); torch.manual_seed(seed); torch.cuda.manual_seed_all(seed)

def collate_skip_none(batch):
    """Drop unreadable/missing samples and collate the rest."""
    batch = [b for b in batch if b is not None]
    if len(batch) == 0:
        return None
    return default_collate(batch)

def _require_smp():
    if smp is None:
        raise ModuleNotFoundError(
            "segmentation_models_pytorch is required for segmentation mode. "
            "Install it or avoid segmentation APIs."
        )

def device_pick():
    if torch.cuda.is_available():
        return torch.device("cuda")
    if getattr(torch.backends, "mps", None) and torch.backends.mps.is_available():
        return torch.device("mps")
    return torch.device("cpu")

def imread_rgb(path: str) -> np.ndarray:
    """Robust RGB reader that handles Unicode/Windows paths.
    Tries cv2.imread → cv2.imdecode(np.fromfile) → PIL.Image.open.
    """
    bgr = cv2.imread(path, cv2.IMREAD_COLOR)
    if bgr is None:
        try:
            data = np.fromfile(path, dtype=np.uint8)
            if data.size > 0:
                bgr = cv2.imdecode(data, cv2.IMREAD_COLOR)
        except Exception:
            bgr = None
    if bgr is not None:
        return cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)
    # PIL fallback
    try:
        with Image.open(path) as img:
            return np.array(img.convert("RGB"))
    except Exception:
        raise FileNotFoundError(path)

def rgb_to_kgray3(rgb_u8: np.ndarray) -> np.ndarray:
    """Convert RGB to CMYK K channel, return as 3ch grayscale uint8.
    K = 255 - max(R,G,B). This matches preprocessing used for tiles.
    """
    k = 255 - np.max(rgb_u8.astype(np.int16), axis=2)
    k = np.clip(k, 0, 255).astype(np.uint8)
    return np.stack([k, k, k], axis=2)

def auto_mask_rgb(rgb: np.ndarray) -> np.ndarray:
    """Heuristic ROI extraction on RGB image.
    Increases saturation, applies CLAHE + Otsu to get foreground, convex hull for robust wafer shape.
    Returns RGB with background zeroed out.
    """
    if rgb is None or rgb.size == 0:
        return rgb
    bgr = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    h, s, v = cv2.split(hsv)
    s = np.clip(s.astype(np.float32) * 2.0, 0, 255).astype(np.uint8)
    hsv_boost = cv2.merge([h, s, v])
    bgr_boost = cv2.cvtColor(hsv_boost, cv2.COLOR_HSV2BGR)
    gray = cv2.cvtColor(bgr_boost, cv2.COLOR_BGR2GRAY)
    gray = cv2.GaussianBlur(gray, (5, 5), 0)
    clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    equalized = clahe.apply(gray)
    _ret, binary = cv2.threshold(equalized, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    contours, _hier = cv2.findContours(binary, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if len(contours) == 0:
        return rgb
    max_contour = max(contours, key=cv2.contourArea)
    hull = cv2.convexHull(max_contour)
    final_mask = np.zeros_like(binary)
    cv2.drawContours(final_mask, [hull], -1, 255, thickness=cv2.FILLED)
    masked = cv2.bitwise_and(bgr_boost, bgr_boost, mask=final_mask)
    return cv2.cvtColor(masked, cv2.COLOR_BGR2RGB)

def compute_fft_channel(rgb_u8: np.ndarray, wafer_mask: Optional[np.ndarray] = None, hp_sigma: float = 0.0, use_gpu: bool = False) -> np.ndarray:
    """Compute 1-channel FFT magnitude from K-gray.
    - Optionally zero out background using wafer_mask before FFT.
    - hp_sigma>0 applies a light unsharp mask style high-pass.
    - use_gpu=True uses torch.fft on GPU (faster for large images).
    Returns uint8 [H,W].
    """
    k = 255 - np.max(rgb_u8.astype(np.int16), axis=2)
    k = np.clip(k, 0, 255).astype(np.uint8)
    if wafer_mask is not None:
        k = k.copy()
        k[~wafer_mask.astype(bool)] = 0
    
    if use_gpu and torch.cuda.is_available():
        # GPU FFT using PyTorch
        k_tensor = torch.from_numpy(k.astype(np.float32)).cuda()
        f = torch.fft.fft2(k_tensor)
        fshift = torch.fft.fftshift(f)
        mag = torch.log1p(torch.abs(fshift))
        mag = mag / (mag.max() + 1e-6)
        mag = (mag * 255).to(torch.uint8).cpu().numpy()
    else:
        # CPU FFT using NumPy
        f = np.fft.fft2(k)
        fshift = np.fft.fftshift(f)
        mag = np.log1p(np.abs(fshift))
        mag = mag / (mag.max() + 1e-6)
        mag = (mag * 255).astype(np.uint8)
    
    if hp_sigma and hp_sigma > 0:
        low = cv2.GaussianBlur(mag, (0, 0), hp_sigma)
        mag = cv2.addWeighted(mag, 1.5, low, -0.5, 0)
        mag = np.clip(mag, 0, 255).astype(np.uint8)
    return mag


def compute_fft_channel_batch_gpu(images: List[np.ndarray], wafer_masks: Optional[List[np.ndarray]] = None) -> List[np.ndarray]:
    """Compute FFT channels for a batch of images on GPU.
    This is more efficient than calling compute_fft_channel repeatedly.
    
    Args:
        images: List of RGB uint8 images [H,W,3]
        wafer_masks: Optional list of wafer masks
    Returns:
        List of FFT magnitude images [H,W] uint8
    """
    if not torch.cuda.is_available():
        # Fallback to CPU
        return [compute_fft_channel(img, mask, 0.0, False) 
                for img, mask in zip(images, wafer_masks or [None]*len(images))]
    
    results = []
    for i, img in enumerate(images):
        k = 255 - np.max(img.astype(np.int16), axis=2)
        k = np.clip(k, 0, 255).astype(np.uint8)
        if wafer_masks is not None and wafer_masks[i] is not None:
            k = k.copy()
            k[~wafer_masks[i].astype(bool)] = 0
        
        k_tensor = torch.from_numpy(k.astype(np.float32)).cuda()
        f = torch.fft.fft2(k_tensor)
        fshift = torch.fft.fftshift(f)
        mag = torch.log1p(torch.abs(fshift))
        mag = mag / (mag.max() + 1e-6)
        mag = (mag * 255).to(torch.uint8).cpu().numpy()
        results.append(mag)
    
    return results

def imread_mask(path: str) -> np.ndarray:
    # Expect single-channel integer mask. If 3ch, convert heuristically.
    img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if img is None:
        raise FileNotFoundError(path)
    if img.ndim == 3:
        # If it's RGB/P, try to convert to single class ids by taking e.g. R channel or unique color mapping.
        # Simple heuristic: if it's paletted PNG, OpenCV already gives 3ch—use first channel.
        img = img[..., 0]
    return img.astype(np.int64)

def save_overlay(rgb: np.ndarray, pred_mask: np.ndarray, out_path: str, palette: Optional[Dict[int, Tuple[int,int,int]]] = None, alpha=0.5):
    """Overlay pseudo-color on RGB. If sizes mismatch, upsample mask to RGB size."""
    rh, rw = rgb.shape[:2]
    mh, mw = pred_mask.shape[:2]
    if (rh, rw) != (mh, mw):
        pred_mask = cv2.resize(pred_mask.astype(np.int32), (rw, rh), interpolation=cv2.INTER_NEAREST).astype(np.int64)
    h, w = pred_mask.shape
    color = np.zeros((h, w, 3), np.uint8)
    uniq = np.unique(pred_mask)
    if palette is None:
        # simple palette
        rng = np.random.default_rng(0)
        for k in uniq:
            if k == 0:
                c = (0,0,0)
            else:
                c = tuple(int(x) for x in rng.integers(0, 255, size=3))
            color[pred_mask==k] = c
    else:
        for k in uniq:
            color[pred_mask==k] = palette.get(int(k), (255,0,255))
    overlay = cv2.addWeighted(cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR), 1.0, color, alpha, 0.0)
    cv2.imwrite(out_path, overlay)


def save_mask_and_overlay(rgb: np.ndarray, pred_mask: np.ndarray, out_base: str,
                          class_names: Optional[List[str]] = None,
                          palette: Optional[Dict[int, Tuple[int,int,int]]] = None,
                          alpha: float = 0.5):
    """Save three files: original image, colorized mask with legend, and overlay.
    - out_base_img.png
    - out_base_mask.png (with legend in top-left)
    - out_base_overlay.png
    """
    rh, rw = rgb.shape[:2]
    mh, mw = pred_mask.shape[:2]
    if (rh, rw) != (mh, mw):
        pred_mask = cv2.resize(pred_mask.astype(np.int32), (rw, rh), interpolation=cv2.INTER_NEAREST).astype(np.int64)

    # Colorize mask
    uniq = [int(u) for u in np.unique(pred_mask)]
    color = np.zeros((rh, rw, 3), np.uint8)
    if palette is None:
        rng = np.random.default_rng(0)
        tmp_palette: Dict[int, Tuple[int,int,int]] = {}
        for k in uniq:
            if k == 0:
                tmp_palette[k] = (0,0,0)
            else:
                tmp_palette[k] = tuple(int(x) for x in rng.integers(0, 255, size=3))
        palette = tmp_palette
    for k in uniq:
        color[pred_mask==k] = palette.get(int(k), (255,0,255))

    # Draw legend on mask image
    mask_with_legend = color.copy()
    # Collect legend items (skip background if desired?)
    items = []
    for k in uniq:
        if class_names is not None and 0 <= k < len(class_names):
            name = class_names[k]
        else:
            name = f"class {k}"
        items.append((k, name, tuple(int(c) for c in palette.get(int(k), (255,0,255)))))

    # Legend layout
    line_h = 20
    pad = 8
    num_lines = len(items)
    box_h = pad*2 + line_h*num_lines
    box_w = 180
    x0, y0 = 10, 10
    x1, y1 = x0 + box_w, y0 + box_h
    cv2.rectangle(mask_with_legend, (x0, y0), (x1, y1), (255,255,255), thickness=-1)
    cv2.rectangle(mask_with_legend, (x0, y0), (x1, y1), (0,0,0), thickness=1)
    for i, (k, name, col) in enumerate(items):
        cy = y0 + pad + i*line_h + 14
        # color chip
        cv2.rectangle(mask_with_legend, (x0+6, cy-12), (x0+26, cy+2), col, thickness=-1)
        cv2.rectangle(mask_with_legend, (x0+6, cy-12), (x0+26, cy+2), (0,0,0), thickness=1)
        # text
        cv2.putText(mask_with_legend, name, (x0+34, cy), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0,0,0), 1, cv2.LINE_AA)

    # Overlay
    overlay = cv2.addWeighted(cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR), 1.0, color, alpha, 0.0)

    # Save files
    cv2.imwrite(f"{out_base}_img.png", cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR))
    cv2.imwrite(f"{out_base}_mask.png", mask_with_legend)
    cv2.imwrite(f"{out_base}_overlay.png", overlay)

def match_pairs(img_dir: str, mask_dir: str) -> List[Tuple[str,str]]:
    """Match by stem (filename without ext)."""
    def _normalize_stem(stem: str) -> str:
        for suf in ("_seg", "_mask", "-seg", "-mask"):
            if stem.endswith(suf):
                return stem[: -len(suf)]
        return stem

    imgs = {}
    for p in glob(os.path.join(img_dir, "*")):
        stem = os.path.splitext(os.path.basename(p))[0]
        imgs[stem] = p
    pairs = []
    for p in glob(os.path.join(mask_dir, "*")):
        stem = os.path.splitext(os.path.basename(p))[0]
        mstem = _normalize_stem(stem)
        if mstem in imgs:
            pairs.append((imgs[mstem], p))
    pairs.sort()
    return pairs

def scan_classes(mask_paths: List[str], sample_max=300) -> List[int]:
    sample = mask_paths if len(mask_paths) <= sample_max else random.sample(mask_paths, sample_max)
    vals = set()
    for mp in sample:
        m = imread_mask(mp)
        vs = np.unique(m)
        for v in vs:
            vals.add(int(v))
    return sorted(list(vals))

def compute_class_weights(mask_paths: List[str], num_classes: int, sample_max=300, eps=1e-6, cap: Optional[float]=None) -> torch.Tensor:
    sample = mask_paths if len(mask_paths) <= sample_max else random.sample(mask_paths, sample_max)
    hist = np.zeros(num_classes, np.float64)
    for mp in sample:
        m = imread_mask(mp)
        m = np.clip(m, 0, num_classes-1)
        h = np.bincount(m.flatten(), minlength=num_classes)
        hist += h
    freq = hist / (hist.sum() + eps)
    w = 1.0 / (np.log(1.02 + freq))  # effective inverse log freq
    if cap is not None:
        w = np.minimum(w, float(cap))
    return torch.tensor(w, dtype=torch.float32)

def ensure_dir(p): 
    os.makedirs(p, exist_ok=True)


def infer_wafer_id(mask_paths: List[str], sample_max: int = 300) -> int:
    """Infer wafer class id as the most frequent non-zero label by pixel count."""
    if not mask_paths:
        return 1
    sample = mask_paths if len(mask_paths) <= sample_max else random.sample(mask_paths, sample_max)
    hist: Optional[np.ndarray] = None
    for mp in sample:
        m = imread_mask(mp)
        counts = np.bincount(m.flatten(), minlength=int(m.max())+1)
        if hist is None:
            hist = counts.astype(np.int64)
        else:
            if counts.size > hist.size:
                hist = np.pad(hist, (0, counts.size - hist.size), constant_values=0)
            elif counts.size < hist.size:
                counts = np.pad(counts, (0, hist.size - counts.size), constant_values=0)
            hist += counts
    if hist is None or hist.size == 0:
        return 1
    hist_no_bg = hist.copy()
    hist_no_bg[0] = 0
    if hist_no_bg.sum() == 0:
        return 1
    wafer_id = int(np.argmax(hist_no_bg))
    return wafer_id


# -----------------------
# ONNL Model Packaging (Classification)
# -----------------------
def _build_onnl_meta_cls(args, class_names: List[str], in_chans: int, epoch: int, best_acc: float,
                         is_best: bool, source: str) -> Dict[str, object]:
    try:
        import datetime as _dt
        ts = _dt.datetime.utcnow().isoformat() + "Z"
    except Exception:
        ts = ""
    meta: Dict[str, object] = {
        "format": "onnlmodel",
        "version": 1,
        "task": "classification",
        "created_at": ts,
        "source": str(source),
        "is_best": bool(is_best),
        "epoch": int(epoch),
        "best_acc": float(best_acc),
        "backbone": str(getattr(args, "backbone", "")),
        "in_chans": int(in_chans),
        "gray_input": bool(getattr(args, "gray_input", False)),
        "add_fft": bool(getattr(args, "add_fft", False)),
        "class_names": list(class_names),
        "num_classes": int(len(class_names)),
        "img_size": int(getattr(args, "img_size", 0) or 0),
        "use_roi": bool(getattr(args, "use_roi", False)),
        "roi_mode": str(getattr(args, "roi_mode", "none")),
        "label_mode": str(getattr(args, "label_mode", "binary")),
        "dataset_mode": str(getattr(args, "dataset_mode", "auto")),
        "balance_sampler": bool(getattr(args, "balance_sampler", True)),
        "balance_aug": bool(getattr(args, "balance_aug", True)),
        "wafer_id": int(getattr(args, "wafer_id", -1)),
        "min_defect_px": int(getattr(args, "min_defect_px", 0)),
        "data_root": str(getattr(args, "data", "")),
        "out_root": str(getattr(args, "out", "")),
        "epochs": int(getattr(args, "epochs", 0)),
        "total_epochs": int(getattr(args, "total_epochs", getattr(args, "epochs", 0) or 0)),
        "resume_from": str(getattr(args, "resume_from", "") or ""),
    }
    return meta

def save_onnlmodel_cls(out_path: str, model: torch.nn.Module, meta: Dict[str, object],
                       optimizer: Optional[torch.optim.Optimizer] = None,
                       scheduler: Optional[torch.optim.lr_scheduler._LRScheduler] = None) -> None:
    obj: Dict[str, object] = {
        "type": "onnlmodel",
        "version": 1,
        "task": "cls",
        "model": model.state_dict(),
        "meta": meta,
    }
    if optimizer is not None:
        try:
            obj["optimizer"] = optimizer.state_dict()
        except Exception:
            pass
    if scheduler is not None:
        try:
            obj["scheduler"] = scheduler.state_dict()
        except Exception:
            pass
    torch.save(obj, out_path)


# -----------------------
# Export — Classification → ONNX
# -----------------------
def export_cls_to_onnx(weights: str, onnx_out_path: str, opset: int = 13) -> str:
    """Export a classification checkpoint (.pt or .onnlmodel) to ONNX.

    Writes two files:
      - <onnx_out_path> (ONNX graph with input name 'input' and output 'logits')
      - <onnx_out_path>.json (sidecar meta with class_names, in_chans, img_size)

    Returns the ONNX path.
    """
    import json as _json
    ensure_dir(os.path.dirname(os.path.abspath(onnx_out_path)) or ".")
    ckpt = torch.load(weights, map_location="cpu")

    # Extract meta
    if isinstance(ckpt, dict) and "meta" in ckpt and isinstance(ckpt.get("meta"), dict):
        meta = ckpt["meta"]
        backbone = str(meta.get("backbone"))
        class_names = list(meta.get("class_names", []))
        in_ch = int(meta.get("in_chans", 3))
        img_size = int(meta.get("img_size", 512) or 512)
        gray_input = bool(meta.get("gray_input", False))
        add_fft = bool(meta.get("add_fft", False))
    else:
        # Legacy .pt
        backbone = ckpt["backbone"]
        class_names = list(ckpt["class_names"])  # defines output dim
        in_ch = int(ckpt.get("in_chans", 3))
        img_size = int(ckpt.get("img_size", 512) or 512)
        gray_input = bool(ckpt.get("gray_input", False))
        add_fft = bool(in_ch in (2, 4))
    n_classes = int(len(class_names))

    # Rebuild model and load weights
    dev = torch.device("cpu")
    model = timm.create_model(backbone, pretrained=False, num_classes=n_classes, in_chans=in_ch)
    model.load_state_dict(ckpt["model"], strict=True)
    model.to(dev).eval()

    # Dummy input (export with fixed square size = img_size)
    x = torch.zeros(1, in_ch, img_size, img_size, dtype=torch.float32, device=dev)
    dynamic_axes = {"input": {0: "batch"}, "logits": {0: "batch"}}
    torch.onnx.export(
        model,
        x,
        onnx_out_path,
        export_params=True,
        opset_version=int(opset),
        do_constant_folding=True,
        input_names=["input"],
        output_names=["logits"],
        dynamic_axes=dynamic_axes,
    )

    # Sidecar meta JSON for C# consumer
    meta_sidecar = {
        "class_names": class_names,
        "in_chans": int(in_ch),
        "img_size": int(img_size),
        "gray_input": bool(gray_input),
        "add_fft": bool(add_fft),
        "backbone": str(backbone),
    }
    try:
        with open(onnx_out_path + ".json", "w", encoding="utf-8") as f:
            _json.dump(meta_sidecar, f, ensure_ascii=False, indent=2)
    except Exception:
        pass
    return onnx_out_path


def convert_onnl_to_onnx_b64(weights: str, opset: int = 13):
    """Convert a classification .onnlmodel/.pt to ONNX and return (onnx_b64_str, meta_dict).

    No file is written; ONNX bytes are returned as base64 string for easy interop.
    """
    import io as _io
    import base64 as _b64
    ckpt = torch.load(weights, map_location="cpu")
    # Extract meta
    if isinstance(ckpt, dict) and "meta" in ckpt and isinstance(ckpt.get("meta"), dict):
        meta = ckpt["meta"]
        backbone = str(meta.get("backbone"))
        class_names = list(meta.get("class_names", []))
        in_ch = int(meta.get("in_chans", 3))
        img_size = int(meta.get("img_size", 512) or 512)
        gray_input = bool(meta.get("gray_input", False))
        add_fft = bool(meta.get("add_fft", False))
    else:
        backbone = ckpt["backbone"]
        class_names = list(ckpt["class_names"])  # defines output dim
        in_ch = int(ckpt.get("in_chans", 3))
        img_size = int(ckpt.get("img_size", 512) or 512)
        gray_input = bool(ckpt.get("gray_input", False))
        add_fft = bool(in_ch in (2, 4))
    n_classes = int(len(class_names))

    dev = torch.device("cpu")
    model = timm.create_model(backbone, pretrained=False, num_classes=n_classes, in_chans=in_ch)
    model.load_state_dict(ckpt["model"], strict=True)
    model.to(dev).eval()

    x = torch.zeros(1, in_ch, img_size, img_size, dtype=torch.float32, device=dev)
    buf = _io.BytesIO()
    dynamic_axes = {"input": {0: "batch"}, "logits": {0: "batch"}}
    torch.onnx.export(
        model,
        x,
        buf,
        export_params=True,
        opset_version=int(opset),
        do_constant_folding=True,
        input_names=["input"],
        output_names=["logits"],
        dynamic_axes=dynamic_axes,
    )
    meta_sidecar = {
        "class_names": class_names,
        "in_chans": int(in_ch),
        "img_size": int(img_size),
        "gray_input": bool(gray_input),
        "add_fft": bool(add_fft),
        "backbone": str(backbone),
    }
    onnx_b64 = _b64.b64encode(buf.getvalue()).decode("ascii")
    return onnx_b64, meta_sidecar


def pack_onnlmodel_container(weights: str, out_path: str | None = None, opset: int = 13) -> str:
    """Create a zipped .onnlmodel container that embeds ONNX and meta.json for C# runtime use.

    - Input 'weights' can be existing .onnlmodel or .pt checkpoint.
    - Output is a single file (recommended to keep extension '.onnlmodel') structured as ZIP with:
        - model.onnx (binary ONNX graph)
        - meta.json  (JSON with class_names/in_chans/img_size/gray_input/add_fft/backbone)
    - Returns the written path.
    """
    import os as _os, json as _json, zipfile as _zip, base64 as _b64
    onnx_b64, meta = convert_onnl_to_onnx_b64(weights, opset=opset)
    onnx_bytes = _b64.b64decode(onnx_b64)
    if out_path is None:
        base, _ext = _os.path.splitext(weights)
        out_path = f"{base}.onnlmodel"  # keep extension
    # Write ZIP
    _os.makedirs(_os.path.dirname(_os.path.abspath(out_path)) or ".", exist_ok=True)
    with _zip.ZipFile(out_path, mode="w", compression=_zip.ZIP_DEFLATED) as zf:
        zf.writestr("model.onnx", onnx_bytes)
        zf.writestr("meta.json", _json.dumps(meta, ensure_ascii=False, indent=2))
    return out_path


def pack_onnlmodel_from_pt(weights_pt: str, out_path: str, opset: int = 13) -> str:
    """Pack a .pt checkpoint into .onnlmodel (ZIP with model.onnx + meta.json) by exporting to ONNX first.

    Same flow as train.py /export/cls/onnl_pack: export_cls_to_onnx then zip. Guarantees a real
    .onnlmodel file (ZIP containing ONNX), not a .pt file.
    """
    import tempfile
    import zipfile as _zip
    import json as _json
    out_path = os.path.abspath(out_path)
    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)
    with tempfile.TemporaryDirectory() as td:
        onnx_file = os.path.join(td, "model.onnx")
        export_cls_to_onnx(weights=weights_pt, onnx_out_path=onnx_file, opset=opset)
        with open(onnx_file, "rb") as f:
            onnx_bytes = f.read()
        meta_path = onnx_file + ".json"
        if os.path.isfile(meta_path):
            with open(meta_path, "r", encoding="utf-8") as f:
                meta = _json.load(f)
        else:
            meta = {}
        with _zip.ZipFile(out_path, mode="w", compression=_zip.ZIP_DEFLATED) as zf:
            zf.writestr("model.onnx", onnx_bytes)
            zf.writestr("meta.json", _json.dumps(meta, ensure_ascii=False, indent=2))
    return out_path


# -----------------------
# Datasets
# -----------------------
class SegDataset(Dataset):
    def __init__(self, pairs: List[Tuple[str,str]], img_size=768, three_classes=False, wafer_id=1, aug=True,
                 pos_crop: float = 0.0, min_defect_px_in_tile: int = 24, crop_first: bool = True,
                 boundary_ignore_px: int = 0, rand_thicken_p: float = 0.0, thicken_radius: int = 1,
                 ignore_index_value: int = 255,
                 strong_defect_aug: bool = False,
                 input_mode: str = "rgb"):
        self.pairs = pairs
        self.img_size = int(img_size)
        self.aug = aug
        self.three = three_classes
        self.wafer_id = int(wafer_id)
        self.pos_crop = float(pos_crop)
        self.min_defect_px_in_tile = int(min_defect_px_in_tile)
        self.crop_first = bool(crop_first)
        self.boundary_ignore_px = int(boundary_ignore_px)
        self.rand_thicken_p = float(rand_thicken_p)
        self.thicken_radius = int(thicken_radius)
        self.ignore_index_value = int(ignore_index_value)
        self.strong_defect_aug = bool(strong_defect_aug)
        self.input_mode = str(input_mode).lower()

        # Transforms applied after cropping from full resolution
        if aug:
            self.tf_light = A.Compose([
                A.ShiftScaleRotate(shift_limit=0.02, scale_limit=0.05, rotate_limit=10,
                                   border_mode=cv2.BORDER_REFLECT_101, p=0.5),
                A.HorizontalFlip(p=0.5),
                A.PadIfNeeded(min_height=self.img_size, min_width=self.img_size,
                               border_mode=cv2.BORDER_REFLECT_101),
                A.CenterCrop(self.img_size, self.img_size),
                A.Normalize(),
                ToTensorV2(),
            ])
            # Stronger defect-focused augmentations (only applied when tile contains defects)
            self.tf_strong = A.Compose([
                A.ShiftScaleRotate(shift_limit=0.03, scale_limit=0.08, rotate_limit=15,
                                   border_mode=cv2.BORDER_REFLECT_101, p=0.7),
                A.HorizontalFlip(p=0.5),
                A.VerticalFlip(p=0.2),
                A.RandomBrightnessContrast(0.2, 0.2, p=0.4),
                A.GaussNoise(var_limit=(5.0, 30.0), p=0.25),
                A.GaussianBlur(blur_limit=(3,5), p=0.15),
                A.PadIfNeeded(min_height=self.img_size, min_width=self.img_size,
                               border_mode=cv2.BORDER_REFLECT_101),
                A.CenterCrop(self.img_size, self.img_size),
                A.Normalize(),
                ToTensorV2(),
            ])
        else:
            self.tf_light = A.Compose([
                A.PadIfNeeded(min_height=self.img_size, min_width=self.img_size,
                               border_mode=cv2.BORDER_REFLECT_101),
                A.CenterCrop(self.img_size, self.img_size),
                A.Normalize(),
                ToTensorV2(),
            ])
            self.tf_strong = self.tf_light

    def _collapse_to_three(self, m: np.ndarray) -> np.ndarray:
        # 0: background, 1: wafer, 2: defect(merged)
        out = np.zeros_like(m, dtype=np.int64)
        out[m==self.wafer_id] = 1
        out[(m!=0) & (m!=self.wafer_id)] = 2
        return out

    def __len__(self): return len(self.pairs)

    def __getitem__(self, idx):
        img_p, mask_p = self.pairs[idx]
        try:
            img = imread_rgb(img_p)
            m = imread_mask(mask_p)
        except Exception as e:
            print(f"[Data][SKIP] unreadable sample: img={img_p} mask={mask_p} ({e})")
            return None

        if self.three:
            m = self._collapse_to_three(m)

        # Crop from full resolution first
        need_pos = (np.random.rand() < self.pos_crop)
        if self.crop_first:
            img, m = self._crop_from_fullres(img, m, self.img_size, need_pos)
        else:
            # Fallback to center crop of resized/padded image (legacy path)
            h, w = m.shape
            x0 = max(0, (w - self.img_size)//2); y0 = max(0, (h - self.img_size)//2)
            img = img[y0:y0+self.img_size, x0:x0+self.img_size]
            m   = m[y0:y0+self.img_size, x0:x0+self.img_size]

        # Slightly thicken defects during training to preserve thin structures
        if self.aug:
            m = self._random_thicken_defect(m)

        # Optional boundary-ignore band
        m = self._mark_boundary_ignore(m, ignore_index=self.ignore_index_value)

        # Optional input mode transform (apply only when we explicitly want to convert RGB → K)
        if self.input_mode == "kgray":
            img = rgb_to_kgray3(img)
        # For "kgray_precomputed", images are already K-gray 3ch on disk, so skip here.

        # Choose augmentation strength depending on presence of defects
        if self.aug and self.strong_defect_aug:
            has_defect = ((m != 0) & (m != self.wafer_id)).any()
            tf = self.tf_strong if has_defect else self.tf_light
        else:
            tf = self.tf_light
        res = tf(image=img, mask=m)
        x, y = res["image"], res["mask"].long()
        return x, y, os.path.basename(img_p)

    def _crop_from_fullres(self, img: np.ndarray, m: np.ndarray, size: int, need_pos: bool) -> Tuple[np.ndarray, np.ndarray]:
        H, W = m.shape
        for _ in range(16):
            if need_pos:
                ys, xs = np.where(m == 2)
                if len(xs) == 0:
                    need_pos = False
                else:
                    i = np.random.randint(len(xs))
                    cx, cy = int(xs[i]), int(ys[i])
                    x0 = int(np.clip(cx - size // 2, 0, max(0, W - size)))
                    y0 = int(np.clip(cy - size // 2, 0, max(0, H - size)))
            if not need_pos:
                x0 = np.random.randint(0, max(1, W - size + 1))
                y0 = np.random.randint(0, max(1, H - size + 1))
            x1, y1 = x0 + size, y0 + size
            crop_m = m[y0:y1, x0:x1]
            if (not need_pos) or (int((crop_m == 2).sum()) >= self.min_defect_px_in_tile):
                crop_img = img[y0:y1, x0:x1]
                return crop_img, crop_m
        return img[y0:y1, x0:x1], m[y0:y1, x0:x1]

    def _random_thicken_defect(self, m: np.ndarray) -> np.ndarray:
        if np.random.rand() > self.rand_thicken_p:
            return m
        k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2*self.thicken_radius+1, 2*self.thicken_radius+1))
        wafer = (m == 1)
        defect = (m == 2).astype(np.uint8)
        dil = cv2.dilate(defect, k, iterations=1).astype(bool)
        out = m.copy()
        out[dil & (wafer | defect.astype(bool))] = 2
        return out

    def _mark_boundary_ignore(self, m: np.ndarray, ignore_index: int) -> np.ndarray:
        if self.boundary_ignore_px <= 0:
            return m
        d = (m == 2).astype(np.uint8) * 255
        edges = cv2.Canny(d, 50, 150)
        band = cv2.dilate(edges, cv2.getStructuringElement(cv2.MORPH_ELLIPSE,
                        (2*self.boundary_ignore_px+1, 2*self.boundary_ignore_px+1)), 1).astype(bool)
        out = m.copy()
        out[band] = ignore_index
        return out


def build_items_from_class_dirs(class_dirs: Dict[str, List[str]]) -> Tuple[List[Tuple[str, str]], List[str]]:
    items: List[Tuple[str, str]] = []
    class_names: List[str] = []
    exts = (".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff")
    for label, paths in class_dirs.items():
        label_u = str(label).upper()
        if label_u not in class_names:
            class_names.append(label_u)
        for p in paths:
            if not os.path.isdir(p):
                continue
            for root, _, files in os.walk(p):
                for fn in files:
                    if fn.lower().endswith(exts):
                        items.append((os.path.join(root, fn), label_u))
    return items, class_names


class ClsDataset(Dataset):
    """
    분류 라벨 규칙:
    - meta/<stem>.json에 "category"가 있으면 그걸 사용 (e.g., SCRATCH, PARTICLE...)
    - 없으면 seg 마스크에서 wafer_id 외의 픽셀 존재 시 'DEFECT', 아니면 'OK'
    """
    def __init__(self, img_dir: str, mask_dir: str, meta_dir: str, img_size=512, wafer_id=1, min_defect_px=32, aug=True,
                 use_roi: bool = False, roi_mode: str = "none", label_mode: str = "binary",
                 folders_only: bool = False, add_fft: bool = False, gray_input: bool = False,
                 geom_aug: Optional[dict] = None, color_aug: Optional[dict] = None, noise_aug: Optional[dict] = None,
                 strong_aug: bool = False, folders_root: Optional[str] = None,
                 items_override: Optional[List[Tuple[str, str]]] = None,
                 class_names_override: Optional[List[str]] = None):
        pairs = match_pairs(img_dir, mask_dir)
        self.items = []
        cat_set = set()
        label_mode = str(label_mode).lower()
        self.label_mode = label_mode
        self.folders_only = bool(folders_only)
        base = os.path.dirname(img_dir)
        if items_override is not None:
            for img_p, label in items_override:
                label_u = str(label).upper()
                self.items.append((img_p, label_u))
                cat_set.add(label_u)
            if class_names_override is not None and len(class_names_override) > 0:
                self.class_names = [str(c).upper() for c in class_names_override]
            else:
                self.class_names = sorted(list(cat_set))
            self.cls2id = {c:i for i,c in enumerate(self.class_names)}
        # Prefer pure folder-based cls if requested
        elif self.folders_only:
            # If explicit root is provided, use it directly
            if folders_root:
                roots: List[str] = [folders_root]
            else:
                cls_raw = os.path.join(base, "cls", "raw")
                roots: List[str] = []
                if os.path.isdir(cls_raw):
                    roots.append(cls_raw)
                # Fallback: allow using <data>/ itself as category root when cls/raw/ is not present
                if not roots and os.path.isdir(base):
                    roots.append(base)
            for root in roots:
                for cat in sorted(os.listdir(root)):
                    cp = os.path.join(root, cat)
                    if not os.path.isdir(cp):
                        continue
                    for fn in os.listdir(cp):
                        if fn.lower().endswith((".png",".jpg",".jpeg",".bmp",".tif",".tiff")):
                            self.items.append((os.path.join(cp, fn), str(cat).upper()))
                            cat_set.add(str(cat).upper())
            self.class_names = sorted(list(cat_set))
            self.cls2id = {c:i for i,c in enumerate(self.class_names)}
        elif len(pairs) == 0:
            base = os.path.dirname(img_dir)
            rok = os.path.join(base, "raw_ok"); sok = os.path.join(base, "seg_ok")
            rng = os.path.join(base, "raw_ng"); sng = os.path.join(base, "seg_ng")
            pairs_ok = match_pairs(rok, sok) if (os.path.isdir(rok) and os.path.isdir(sok)) else []
            pairs_ng = match_pairs(rng, sng) if (os.path.isdir(rng) and os.path.isdir(sng)) else []
            if pairs_ok or pairs_ng:
                if label_mode == "category":
                    # Try to use meta category if present; else OK/DEFECT by folder
                    for img_p, _ in pairs_ok:
                        stem = os.path.splitext(os.path.basename(img_p))[0]
                        meta_p = os.path.join(meta_dir, f"{stem}.json")
                        label = "OK"
                        if os.path.exists(meta_p):
                            try:
                                with open(meta_p, "r", encoding="utf-8") as f:
                                    meta = json.load(f)
                                if meta.get("category"):
                                    label = str(meta["category"]).upper()
                            except Exception:
                                pass
                        self.items.append((img_p, label)); cat_set.add(label)
                    for img_p, _ in pairs_ng:
                        stem = os.path.splitext(os.path.basename(img_p))[0]
                        meta_p = os.path.join(meta_dir, f"{stem}.json")
                        label = "DEFECT"
                        if os.path.exists(meta_p):
                            try:
                                with open(meta_p, "r", encoding="utf-8") as f:
                                    meta = json.load(f)
                                if meta.get("category"):
                                    label = str(meta["category"]).upper()
                            except Exception:
                                pass
                        self.items.append((img_p, label)); cat_set.add(label)
                else:
                    for img_p, _ in pairs_ok:
                        self.items.append((img_p, "OK")); cat_set.add("OK")
                    for img_p, _ in pairs_ng:
                        self.items.append((img_p, "DEFECT")); cat_set.add("DEFECT")
            else:
                # Fallback to meta/mask-driven labeling if even ok/ng not present
                for img_p, m_p in pairs:
                    stem = os.path.splitext(os.path.basename(img_p))[0]
                    meta_p = os.path.join(meta_dir, f"{stem}.json")
                    if label_mode == "category" and os.path.exists(meta_p):
                        try:
                            with open(meta_p, "r", encoding="utf-8") as f:
                                meta = json.load(f)
                            label = str(meta.get("category", "")).upper() or None
                        except Exception:
                            label = None
                    else:
                        label = None
                    if label is None:
                        m = imread_mask(m_p)
                        defect_px = int(((m != 0) & (m != wafer_id)).sum())
                        label = ("DEFECT" if defect_px >= min_defect_px else "OK")
                    self.items.append((img_p, label))
                    cat_set.add(label)
        else:
            # Standard path using raw/seg + meta or mask-derived
            for img_p, m_p in pairs:
                stem = os.path.splitext(os.path.basename(img_p))[0]
                meta_p = os.path.join(meta_dir, f"{stem}.json")
                if label_mode == "category" and os.path.exists(meta_p):
                    try:
                        with open(meta_p, "r", encoding="utf-8") as f:
                            meta = json.load(f)
                        label = str(meta.get("category", "")).upper() or None
                    except Exception:
                        label = None
                else:
                    label = None
                if label is None:
                    m = imread_mask(m_p)
                    defect_px = int(((m != 0) & (m != wafer_id)).sum())
                    label = ("DEFECT" if defect_px >= min_defect_px else "OK")
                self.items.append((img_p, label))
                cat_set.add(label)
        if items_override is None:
            self.class_names = sorted(list(cat_set))  # map to idx
            self.cls2id = {c:i for i,c in enumerate(self.class_names)}
        self.img_size = img_size
        self.mask_dir = mask_dir
        self.base_dir = os.path.dirname(img_dir)
        self.wafer_id = int(wafer_id)
        self.use_roi = bool(use_roi)
        self.roi_mode = str(roi_mode).lower()
        self.add_fft = bool(add_fft)
        self.gray_input = bool(gray_input)
        base_ch = 1 if self.gray_input else 3
        self.in_ch = base_ch + (1 if self.add_fft else 0)
        # Transforms
        if aug:
            if build_cls_transforms is not None and (geom_aug or color_aug or noise_aug):
                # Build from DTO-like dicts
                try:
                    self.tf = build_cls_transforms(img_size, self.in_ch, geom_aug, color_aug, noise_aug, strong=False)
                    self.tf_strong = build_cls_transforms(img_size, self.in_ch, geom_aug, color_aug, noise_aug, strong=True)
                except Exception:
                    # Fallback to neutral (no random aug) if builder fails
                    self.tf = A.Compose([
                        A.LongestMaxSize(max_size=img_size),
                        A.PadIfNeeded(min_height=img_size, min_width=img_size, border_mode=cv2.BORDER_REFLECT_101),
                        A.CenterCrop(img_size, img_size),
                        A.Normalize(mean=tuple([0.0]*self.in_ch), std=tuple([1.0]*self.in_ch)),
                        ToTensorV2(),
                    ])
                    self.tf_strong = self.tf
            else:
                # No DTO provided: default to neutral transforms (no random aug)
                self.tf = A.Compose([
                    A.LongestMaxSize(max_size=img_size),
                    A.PadIfNeeded(min_height=img_size, min_width=img_size, border_mode=cv2.BORDER_REFLECT_101),
                    A.CenterCrop(img_size, img_size),
                    A.Normalize(mean=tuple([0.0]*self.in_ch), std=tuple([1.0]*self.in_ch)),
                    ToTensorV2(),
                ])
                self.tf_strong = self.tf
        else:
            self.tf = A.Compose([
                A.LongestMaxSize(max_size=img_size),
                A.PadIfNeeded(min_height=img_size, min_width=img_size, border_mode=cv2.BORDER_REFLECT_101),
                A.CenterCrop(img_size, img_size),
                A.Normalize(mean=tuple([0.0]*self.in_ch), std=tuple([1.0]*self.in_ch)),
                ToTensorV2(),
            ])
            self.tf_strong = self.tf
        # Minority augmentation controls (set by trainer)
        self.use_minority_strong_aug: bool = False
        self.minority_classes: set = set()

    def __len__(self): return len(self.items)

    def __getitem__(self, idx):
        img_p, label = self.items[idx]
        try:
            x = imread_rgb(img_p)
        except Exception as e:
            print(f"[Data][SKIP] unreadable cls image: {img_p} ({e})")
            return None
        if self.use_roi and not self.folders_only:
            if self.roi_mode == "mask":
                stem = os.path.splitext(os.path.basename(img_p))[0]
                mask_dirs = [
                    self.mask_dir,
                    os.path.join(self.base_dir, "seg_ok"),
                    os.path.join(self.base_dir, "seg_ng"),
                ]
                m_p = None
                for md in mask_dirs:
                    cand = [
                        os.path.join(md, f"{stem}.png"),
                        os.path.join(md, f"{stem}_seg.png"),
                        os.path.join(md, f"{stem}-seg.png"),
                        os.path.join(md, f"{stem}_mask.png"),
                    ]
                    for cp in cand:
                        if os.path.exists(cp):
                            m_p = cp; break
                    if m_p is not None:
                        break
                if m_p is not None:
                    try:
                        m = imread_mask(m_p)
                    except Exception as e:
                        print(f"[Data][SKIP] unreadable cls mask: {m_p} ({e})")
                        return None
                    wafer = (m == self.wafer_id)
                    x = x.copy()
                    x[~wafer] = 0
            elif self.roi_mode == "auto":
                x = auto_mask_rgb(x)
        # Build base channels (gray or rgb)
        if self.gray_input:
            # K-gray 1ch base
            k = 255 - np.max(x.astype(np.int16), axis=2)
            k = np.clip(k, 0, 255).astype(np.uint8)
            base = k[..., None]
        else:
            base = x
        # Append FFT channel if enabled
        if self.add_fft:
            fft_ch = compute_fft_channel(x, wafer_mask=None, hp_sigma=0.0)
            x = np.concatenate([base, fft_ch[..., None]], axis=2)
        else:
            x = base
        # Choose transform strength based on minority
        if self.use_minority_strong_aug and label in self.minority_classes:
            x = self.tf_strong(image=x)["image"]
        else:
            x = self.tf(image=x)["image"]
        y = self.cls2id[label]
        return x, y, os.path.basename(img_p)


# -----------------------
# Loss / Metrics
# -----------------------
class DiceLoss(nn.Module):
    def __init__(self, smooth=1.0, ignore_index=None):
        super().__init__()
        self.smooth = smooth
        self.ignore_index = ignore_index

    def forward(self, logits, targets):
        # logits: (B,C,H,W), targets: (B,H,W)
        num_classes = logits.shape[1]
        probs = torch.softmax(logits, dim=1)
        # Clamp targets to valid range BEFORE one_hot to avoid OOB when ignore_index is used
        targets_safe = targets.clamp(min=0, max=num_classes-1)
        targets_oh = F.one_hot(targets_safe, num_classes=num_classes).permute(0,3,1,2).float()
        if self.ignore_index is not None and 0 <= self.ignore_index < num_classes:
            mask = (targets != self.ignore_index).float().unsqueeze(1)  # (B,1,H,W)
            probs = probs * mask
            targets_oh = targets_oh * mask
        dims = (0,2,3)
        inter = (probs * targets_oh).sum(dims)
        union = probs.sum(dims) + targets_oh.sum(dims)
        dice = (2*inter + self.smooth) / (union + self.smooth)
        return 1 - dice.mean()


class TverskyDefectLoss(nn.Module):
    def __init__(self, defect_class=2, alpha=0.3, beta=0.7, smooth=1.0, ignore_index=None):
        super().__init__()
        self.c = defect_class
        self.a = alpha; self.b = beta; self.smooth = smooth
        self.ignore_index = ignore_index
    def forward(self, logits, targets):
        probs = torch.softmax(logits, dim=1)[:, self.c:self.c+1, ...]
        with torch.no_grad():
            t = (targets==self.c).float().unsqueeze(1)
            if self.ignore_index is not None:
                mask = (targets!=self.ignore_index).float().unsqueeze(1)
                probs = probs * mask
                t = t * mask
        TP = (probs*t).sum(dim=(0,2,3))
        FP = (probs*(1.0-t)).sum(dim=(0,2,3))
        FN = ((1.0-probs)*t).sum(dim=(0,2,3))
        tversky = (TP + self.smooth) / (TP + self.a*FP + self.b*FN + self.smooth)
        return 1.0 - tversky.mean()

def iou_per_class(pred: torch.Tensor, target: torch.Tensor, num_classes:int) -> List[float]:
    # pred/target: (B,H,W) int
    ious = []
    for c in range(num_classes):
        p = (pred==c); t=(target==c)
        inter = (p & t).sum().item()
        union = (p | t).sum().item()
        if union == 0:
            ious.append(float('nan'))
        else:
            ious.append(inter/union)
    return ious


# -----------------------
# Training — Segmentation
# -----------------------
def train_seg(args):
    set_seed(args.seed)
    dev = device_pick()
    print(f"[Device] {dev}")

    # Support balanced OK/NG split folders if present; else fallback to raw/seg
    img_dir = os.path.join(args.data, "raw")
    mask_dir = os.path.join(args.data, "seg")
    viz_dir  = os.path.join(args.data, "viz")
    ensure_dir(viz_dir)

    rok = os.path.join(args.data, "raw_ok"); sok = os.path.join(args.data, "seg_ok")
    rng = os.path.join(args.data, "raw_ng"); sng = os.path.join(args.data, "seg_ng")
    pairs: List[Tuple[str,str]] = []
    if os.path.isdir(rok) and os.path.isdir(sok) and os.path.isdir(rng) and os.path.isdir(sng):
        pairs_ok = match_pairs(rok, sok)
        pairs_ng = match_pairs(rng, sng)
        pairs = pairs_ok + pairs_ng
        if not pairs:
            pairs = match_pairs(img_dir, mask_dir)
    else:
        pairs = match_pairs(img_dir, mask_dir)
    if len(pairs) < 8:
        raise RuntimeError("데이터가 너무 적습니다(최소 8쌍 권장).")

    # split
    random.shuffle(pairs)
    n = len(pairs)
    n_val = max( max(1,int(n*0.15)), 8 )
    val_pairs = pairs[:n_val]
    train_pairs = pairs[n_val:]

    # wafer-id (auto if negative)
    wafer_id = args.wafer_id
    if wafer_id < 0:
        wafer_id = infer_wafer_id([mp for _, mp in pairs])
        print(f"[Auto wafer-id] inferred wafer_id={wafer_id}")

    # dataset
    train_ds = SegDataset(
        train_pairs,
        img_size=args.img_size,
        three_classes=args.three_classes,
        wafer_id=wafer_id,
        aug=True,
        pos_crop=args.pos_crop,
        min_defect_px_in_tile=args.min_defect_px_in_tile,
        crop_first=True,
        boundary_ignore_px=args.boundary_ignore_px,
        rand_thicken_p=args.rand_thicken_p,
        thicken_radius=args.thicken_radius,
        ignore_index_value=args.ignore_index if args.ignore_index>=0 else 255,
        strong_defect_aug=args.strong_defect_aug,
        input_mode=args.input_mode,
    )
    val_ds   = SegDataset(
        val_pairs,
        img_size=args.img_size,
        three_classes=args.three_classes,
        wafer_id=wafer_id,
        aug=False,
        pos_crop=0.0,
        min_defect_px_in_tile=args.min_defect_px_in_tile,
        crop_first=True,
        boundary_ignore_px=0,
        rand_thicken_p=0.0,
        thicken_radius=args.thicken_radius,
        ignore_index_value=args.ignore_index if args.ignore_index>=0 else 255,
        strong_defect_aug=False,
        input_mode=args.input_mode,
    )

    # num classes
    # If three_classes: fixed 3; else infer from masks (max label + 1 capped)
    if args.three_classes:
        num_classes = 3
    else:
        cls_vals = scan_classes([mp for _, mp in pairs])
        num_classes = int(max(cls_vals)+1)
        print(f"[Class scan] labels in data: {cls_vals} → num_classes={num_classes}")

    # loaders
    pin = (dev.type == "cuda")
    if dev.type == "mps": pin = False
    # Optional oversampling: weight tiles with defects more than OK tiles
    if args.defect_oversample > 1.0:
        labels = []  # 1 if contains defect, else 0
        for ip, mp in train_pairs:
            m = imread_mask(mp)
            wafer_id_eff = wafer_id
            has_def = int(((m != 0) & (m != wafer_id_eff)).any())
            labels.append(has_def)
        labels = np.array(labels, dtype=np.int64)
        w_ok = 1.0
        w_def = float(args.defect_oversample)
        weights = np.where(labels==1, w_def, w_ok).astype(np.float64)
        sampler = WeightedRandomSampler(torch.from_numpy(weights).double(), num_samples=len(weights), replacement=True)
        train_ld = DataLoader(train_ds, batch_size=args.batch_size, sampler=sampler, num_workers=args.workers, pin_memory=pin, drop_last=True, collate_fn=collate_skip_none)
    else:
        train_ld = DataLoader(train_ds, batch_size=args.batch_size, shuffle=True, num_workers=args.workers, pin_memory=pin, drop_last=True, collate_fn=collate_skip_none)
    val_ld   = DataLoader(val_ds,   batch_size=max(1,args.batch_size//2), shuffle=False, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)

    _require_smp()
    # model
    arch = args.arch.lower()
    encoder = args.encoder
    if arch == "deeplabv3plus":
        model = smp.DeepLabV3Plus(encoder_name=encoder, encoder_weights="imagenet", classes=num_classes, activation=None)
    elif arch == "unet":
        model = smp.Unet(encoder_name=encoder, encoder_weights="imagenet", classes=num_classes, activation=None)
    elif arch == "unetplusplus":
        model = smp.UnetPlusPlus(encoder_name=encoder, encoder_weights="imagenet", classes=num_classes, activation=None)
    elif arch == "fpn":
        model = smp.FPN(encoder_name=encoder, encoder_weights="imagenet", classes=num_classes, activation=None)
    else:
        raise ValueError(f"Unknown arch: {arch}")
    model.to(dev)

    # loss
    ce_weight = None
    if args.class_weights:
        ce_weight = compute_class_weights([mp for _, mp in pairs], num_classes=num_classes, cap=args.ce_weight_cap).to(dev)
        print("[Class weights]", ce_weight.cpu().numpy())

    if args.ignore_index >= 0:
        ce = nn.CrossEntropyLoss(weight=ce_weight, ignore_index=args.ignore_index)
    else:
        ce = nn.CrossEntropyLoss(weight=ce_weight)
    dice = DiceLoss(ignore_index=args.ignore_index if args.ignore_index>=0 else None)
    tvd  = TverskyDefectLoss(defect_class=2, alpha=0.3, beta=0.7,
                             ignore_index=args.ignore_index if args.ignore_index>=0 else None)

    opt = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    sched = torch.optim.lr_scheduler.CosineAnnealingLR(opt, T_max=args.epochs)

    scaler = torch.amp.GradScaler('cuda', enabled=(dev.type=="cuda" and args.amp))

    best_miou = -1.0
    best_path = os.path.join(args.out, f"seg_{arch}_{encoder}_best.pt")
    ensure_dir(args.out)

    for epoch in range(1, args.epochs+1):
        t0=time.time()
        model.train()
        tr_loss = 0.0
        tr_seen = 0
        for batch in train_ld:
            if batch is None:
                continue
            x, y, _ = batch
            x = x.to(dev, non_blocking=True); y = y.to(dev, non_blocking=True)
            opt.zero_grad(set_to_none=True)
            with torch.amp.autocast('cuda', enabled=(dev.type=="cuda" and args.amp)):
                logits = model(x)
                loss = (ce(logits, y) * (1.0 - args.dice_weight)
                        + dice(logits, y) * args.dice_weight
                        + 0.3 * tvd(logits, y))
            if scaler.is_enabled():
                scaler.scale(loss).backward()
                scaler.step(opt)
                scaler.update()
            else:
                loss.backward(); opt.step()
            tr_loss += loss.item() * x.size(0)
            tr_seen += x.size(0)
        sched.step()

        # validate
        model.eval()
        val_loss=0.0; iou_sums=np.zeros(num_classes, dtype=np.float64); iou_counts=np.zeros(num_classes, dtype=np.int64)
        val_seen = 0
        with torch.no_grad():
            for batch in val_ld:
                if batch is None:
                    continue
                x, y, names = batch
                x=x.to(dev); y=y.to(dev)
                logits = model(x)
                loss = (ce(logits,y) * (1.0 - args.dice_weight)
                        + dice(logits,y) * args.dice_weight
                        + 0.3 * tvd(logits, y))
                val_loss += loss.item() * x.size(0)
                val_seen += x.size(0)
                pred = torch.argmax(logits, dim=1)
                ious = iou_per_class(pred, y, num_classes)
                for c, v in enumerate(ious):
                    if not math.isnan(v):
                        iou_sums[c] += v; iou_counts[c] += 1

            per_cls_iou = [ (iou_sums[c]/max(1,iou_counts[c])) for c in range(num_classes) ]
            miou = float(np.mean(per_cls_iou)) if per_cls_iou else 0.0

        dt = time.time()-t0
        print(f"[{epoch:03d}] train {tr_loss/max(1,tr_seen):.4f} | val {val_loss/max(1,val_seen):.4f} | mIoU {miou:.3f} | time {dt:.1f}s")
        print("per-class IoU: " + " ".join([f"{i}:{v:.3f}" for i,v in enumerate(per_cls_iou)]))
        # save best
        if miou > best_miou:
            best_miou = miou
            torch.save({"model":model.state_dict(),
                        "arch":arch,"encoder":encoder,"num_classes":num_classes,
                        "three_classes":args.three_classes,"wafer_id":wafer_id}, best_path)
            # quick visualize a few
            model.eval()
            with torch.no_grad():
                for batch in val_ld:
                    if batch is None:
                        continue
                    x, _, names = batch
                    x=x.to(dev)
                    pred = torch.argmax(model(x), dim=1).cpu().numpy()
                    for i in range(min(len(names), 4)):
                        stem = os.path.splitext(names[i])[0]
                        # Prefer loading original RGB if present to avoid normalization artifacts
                        img_path_png = os.path.join(img_dir, f"{stem}.png")
                        img_path_jpg = os.path.join(img_dir, f"{stem}.jpg")
                        if os.path.exists(img_path_png):
                            rgb = imread_rgb(img_path_png)
                        elif os.path.exists(img_path_jpg):
                            rgb = imread_rgb(img_path_jpg)
                        else:
                            xx = (x[i].detach().cpu().numpy().transpose(1,2,0))
                            xx = ((xx - xx.min())/(xx.max()-xx.min()+1e-6)*255).astype(np.uint8)
                            rgb = cv2.cvtColor(xx, cv2.COLOR_BGR2RGB)
                        out_base = os.path.join(viz_dir, f"{stem}_pred")
                        # if three-classes, define simple names
                        class_names = ["bg","wafer","defect"] if args.three_classes else None
                        save_mask_and_overlay(rgb, pred[i], out_base, class_names=class_names)
                    break

    print(f"[Done] Best mIoU={best_miou:.3f} | saved → {best_path}")


# -----------------------
# Training — Classification
# -----------------------
def train_cls(args):
    set_seed(args.seed)
    dev = device_pick()
    print(f"[Device] {dev}")

    img_dir = os.path.join(args.data, "raw")
    mask_dir = os.path.join(args.data, "seg")
    meta_dir = os.path.join(args.data, "meta")

    wafer_id = args.wafer_id
    if wafer_id < 0:
        wafer_id = infer_wafer_id([mp for _, mp in match_pairs(img_dir, mask_dir)])
        print(f"[Auto wafer-id] inferred wafer_id={wafer_id}")

    class_dirs = getattr(args, "class_dirs", None)
    tr_ds = None
    va_ds = None
    if class_dirs:
        items, class_names = build_items_from_class_dirs(class_dirs)
        if len(items) < 1:
            raise RuntimeError("분류 데이터가 너무 적습니다(최소 1장 필요).")
        ds_all = ClsDataset(
            "", "", "",
            img_size=args.img_size,
            wafer_id=wafer_id,
            min_defect_px=args.min_defect_px,
            aug=True,
            use_roi=getattr(args, "use_roi", False),
            roi_mode=getattr(args, "roi_mode", "none"),
            label_mode=getattr(args, "label_mode", "binary"),
            folders_only=True,
            add_fft=bool(getattr(args, "add_fft", False)),
            gray_input=bool(getattr(args, "gray_input", False)),
            geom_aug=getattr(args, "geom_aug", None),
            color_aug=getattr(args, "color_aug", None),
            noise_aug=getattr(args, "noise_aug", None),
            strong_aug=bool(getattr(args, "strong_aug", False)),
            items_override=items,
            class_names_override=class_names,
        )
        idxs = list(range(len(ds_all)))
        random.shuffle(idxs)
        val_ratio = float(getattr(args, "val_split", 0.2))
        val_min = int(getattr(args, "val_min", 32))
        n_val = int(len(ds_all) * max(0.0, min(1.0, val_ratio)))
        n_val = max(n_val, val_min)
        n_val = min(n_val, len(ds_all))
        if val_ratio == 0.0:
            n_val = 0
        val_idx = idxs[:n_val]; tr_idx = idxs[n_val:]
        try:
            print(f"[Split] train={len(tr_idx)} | val={len(val_idx)} | total={len(ds_all)}")
        except Exception:
            pass
        if len(tr_idx) == 0:
            raise RuntimeError("학습 샘플이 없습니다. val_split/val_min을 조정하세요.")
        class_names = ds_all.class_names
        num_classes = len(class_names)
        print("[Classes]", class_names)
        labels = [ds_all.cls2id[ds_all.items[i][1]] for i in tr_idx]
        class_sample_count = np.bincount(labels, minlength=num_classes)
        nz = class_sample_count > 0
        median_cnt = int(np.median(class_sample_count[nz])) if nz.any() else 0
        ds_all.minority_classes = set([class_names[c] for c in range(num_classes) if class_sample_count[c] < median_cnt])
        ds_all.use_minority_strong_aug = bool(getattr(args, "balance_aug", False))
        tr_ds = Subset(ds_all, tr_idx)
        va_ds = Subset(ds_all, val_idx) if n_val > 0 else None
    else:
        mode = str(getattr(args, "dataset_mode", "auto"))
        if mode == "split_folders":
            # Use pre-split folders (preferred: <data>/train/<CAT>, fallback: <data>/cls/train/raw/<CAT>)
            tr_root_pref = os.path.join(args.data, "train")
            va_root_pref = os.path.join(args.data, "val")
            tr_root_legacy = os.path.join(args.data, "cls", "train", "raw")
            va_root_legacy = os.path.join(args.data, "cls", "val", "raw")
            tr_root = tr_root_pref if os.path.isdir(tr_root_pref) else tr_root_legacy
            va_root = va_root_pref if os.path.isdir(va_root_pref) else va_root_legacy
            if not os.path.isdir(tr_root):
                raise RuntimeError(f"split_folders 모드: 학습 폴더가 없습니다: {tr_root}")
            tr_ds = ClsDataset(
                tr_root, mask_dir, meta_dir,
                img_size=args.img_size,
                wafer_id=wafer_id,
                min_defect_px=args.min_defect_px,
                aug=True,
                use_roi=getattr(args, "use_roi", False),
                roi_mode=getattr(args, "roi_mode", "none"),
                label_mode=getattr(args, "label_mode", "binary"),
                folders_only=True,
                add_fft=bool(getattr(args, "add_fft", False)),
                gray_input=bool(getattr(args, "gray_input", False)),
                geom_aug=getattr(args, "geom_aug", None),
                color_aug=getattr(args, "color_aug", None),
                noise_aug=getattr(args, "noise_aug", None),
                strong_aug=bool(getattr(args, "strong_aug", False)),
                folders_root=tr_root,
            )
            if os.path.isdir(va_root):
                va_ds = ClsDataset(
                    va_root, mask_dir, meta_dir,
                    img_size=args.img_size,
                    wafer_id=wafer_id,
                    min_defect_px=args.min_defect_px,
                    aug=False,
                    use_roi=getattr(args, "use_roi", False),
                    roi_mode=getattr(args, "roi_mode", "none"),
                    label_mode=getattr(args, "label_mode", "binary"),
                    folders_only=True,
                    add_fft=bool(getattr(args, "add_fft", False)),
                    gray_input=bool(getattr(args, "gray_input", False)),
                    folders_root=va_root,
                )
            else:
                print(f"[Split] validation folder not found; training without validation: {va_root}")
            # Harmonize class names (union, sorted)
            class_names = sorted(list(set(getattr(tr_ds, "class_names", [])) | set(getattr(va_ds, "class_names", [])) if va_ds else set(getattr(tr_ds, "class_names", []))))
            tr_ds.class_names = class_names; tr_ds.cls2id = {c:i for i,c in enumerate(class_names)}
            if va_ds is not None:
                va_ds.class_names = class_names; va_ds.cls2id = {c:i for i,c in enumerate(class_names)}
            num_classes = len(class_names)
            print("[Classes]", class_names)
            # class counts and balancing from training dataset only
            labels = [tr_ds.cls2id[label] for _, label in tr_ds.items]
            class_sample_count = np.bincount(labels, minlength=num_classes)
            nz = class_sample_count > 0
            median_cnt = int(np.median(class_sample_count[nz])) if nz.any() else 0
            tr_ds.minority_classes = set([class_names[c] for c in range(num_classes) if class_sample_count[c] < median_cnt])
            tr_ds.use_minority_strong_aug = bool(getattr(args, "balance_aug", False))
            # Report dataset sizes in split_folders mode
            try:
                n_tr = len(tr_ds)
                n_va = 0 if va_ds is None else len(va_ds)
                print(f"[SplitFolders] train={n_tr} | val={n_va} | total={n_tr + n_va}")
            except Exception:
                pass
        else:
            # auto or folders (random split)
            ds_all = ClsDataset(
                img_dir, mask_dir, meta_dir,
                img_size=args.img_size,
                wafer_id=wafer_id,
                min_defect_px=args.min_defect_px,
                aug=True,
                use_roi=getattr(args, "use_roi", False),
                roi_mode=getattr(args, "roi_mode", "none"),
                label_mode=getattr(args, "label_mode", "binary"),
                folders_only=bool(mode == "folders") or bool(getattr(args, "folders_only", False)),
                add_fft=bool(getattr(args, "add_fft", False)),
                gray_input=bool(getattr(args, "gray_input", False)),
                geom_aug=getattr(args, "geom_aug", None),
                color_aug=getattr(args, "color_aug", None),
                noise_aug=getattr(args, "noise_aug", None),
                strong_aug=bool(getattr(args, "strong_aug", False)),
            )
            if len(ds_all) < 1:
                raise RuntimeError("분류 데이터가 너무 적습니다(최소 1장 필요).")
            idxs = list(range(len(ds_all)))
            random.shuffle(idxs)
            val_ratio = float(getattr(args, "val_split", 0.2))
            val_min = int(getattr(args, "val_min", 32))
            n_val = int(len(ds_all) * max(0.0, min(1.0, val_ratio)))
            n_val = max(n_val, val_min)
            n_val = min(n_val, len(ds_all))
            if val_ratio == 0.0:
                n_val = 0
            val_idx = idxs[:n_val]; tr_idx = idxs[n_val:]
            try:
                print(f"[Split] train={len(tr_idx)} | val={len(val_idx)} | total={len(ds_all)}")
            except Exception:
                pass
            if len(tr_idx) == 0:
                raise RuntimeError("학습 샘플이 없습니다. val_split/val_min을 조정하세요.")
            class_names = ds_all.class_names
            num_classes = len(class_names)
            print("[Classes]", class_names)
            labels = [ds_all.cls2id[ds_all.items[i][1]] for i in tr_idx]
            class_sample_count = np.bincount(labels, minlength=num_classes)
            nz = class_sample_count > 0
            median_cnt = int(np.median(class_sample_count[nz])) if nz.any() else 0
            ds_all.minority_classes = set([class_names[c] for c in range(num_classes) if class_sample_count[c] < median_cnt])
            ds_all.use_minority_strong_aug = bool(getattr(args, "balance_aug", False))
            tr_ds = Subset(ds_all, tr_idx)
            va_ds = Subset(ds_all, val_idx) if n_val > 0 else None
    # At this point, tr_ds is Dataset or Subset; va_ds optional

    pin = (dev.type == "cuda")
    if dev.type == "mps": pin = False
    if bool(getattr(args, "balance_sampler", True)):
        # build weights from current training dataset
        labels_tr = labels if isinstance(tr_ds, Subset) else [tr_ds.cls2id[label] for _, label in getattr(tr_ds, "items", [])]
        class_sample_count_tr = np.bincount(labels_tr, minlength=num_classes)
        nz_tr = class_sample_count_tr > 0
        inv = np.zeros_like(class_sample_count_tr, dtype=np.float64)
        inv[nz_tr] = class_sample_count_tr[nz_tr].max() / class_sample_count_tr[nz_tr]
        samples_weight = np.array([inv[l] for l in labels_tr])
        sampler = WeightedRandomSampler(torch.from_numpy(samples_weight).double(), num_samples=len(samples_weight), replacement=True)
        tr_ld = DataLoader(tr_ds, batch_size=args.batch_size, sampler=sampler, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)
    else:
        tr_ld = DataLoader(tr_ds, batch_size=args.batch_size, shuffle=True, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)
    va_ld = None if va_ds is None else DataLoader(va_ds, batch_size=max(1,args.batch_size//2), shuffle=False, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)

    # model
    base_ch = 1 if bool(getattr(args, "gray_input", False)) else 3
    in_ch = base_ch + (1 if bool(getattr(args, "add_fft", False)) else 0)
    # Create without online pretrained to avoid network in offline environments
    model = timm.create_model(args.backbone, pretrained=False, num_classes=num_classes, in_chans=in_ch)
    # Try to load local pretrained backbone weights (ignore classifier mismatch)
    try:
        try:
            from timm.models import get_cache_dir, get_pretrained_cfg
        except Exception:
            from timm.models.hub import get_cache_dir  # deprecated path fallback
            from timm.models._registry import get_pretrained_cfg  # best-effort fallback
        cache_dir = get_cache_dir()
        expected = None
        try:
            _cfg = get_pretrained_cfg(args.backbone)
            _url = getattr(_cfg, 'url', None) or (_cfg.get('url') if isinstance(_cfg, dict) else None)
            if _url:
                expected = os.path.join(cache_dir, os.path.basename(str(_url).split('?')[0]))
        except Exception:
            expected = None
        candidates = []
        if expected:
            candidates.append(expected)
        # Additional local search locations
        for base in [os.getcwd(), os.path.dirname(__file__)]:
            local_dir = os.path.join(base, 'models', 'timm')
            if os.path.isdir(local_dir):
                for nm in os.listdir(local_dir):
                    if nm.lower().endswith('.pth'):
                        candidates.append(os.path.join(local_dir, nm))
        ckpt_path = next((p for p in candidates if os.path.exists(p)), None)
        if ckpt_path:
            sd = torch.load(ckpt_path, map_location='cpu')
            if isinstance(sd, dict) and 'state_dict' in sd:
                sd = sd['state_dict']
            # Filter/adapt incompatible shapes (e.g., conv_stem for non-3ch, classifier for non-1000 classes)
            msd = model.state_dict()
            adapted = {}
            for k, v in sd.items():
                if k not in msd:
                    continue
                tv = msd[k]
                if tv.shape == v.shape:
                    adapted[k] = v
                    continue
                # Skip classifier head if shape mismatches
                if "classifier" in k or k.endswith(".weight") and v.ndim == 2:
                    continue
                # Handle first conv stem when input channels differ (e.g., 1, 2, 4)
                if k.endswith("conv_stem.weight") and v.ndim == 4 and v.shape[1] == 3 and tv.ndim == 4 and tv.shape[2:] == v.shape[2:]:
                    try:
                        in_ch_target = tv.shape[1]
                        if in_ch_target == 1:
                            new_w = v.mean(dim=1, keepdim=True)
                        else:
                            new_w = torch.zeros_like(tv)
                            ch_copy = min(3, in_ch_target)
                            new_w[:, :ch_copy] = v[:, :ch_copy]
                            if in_ch_target > ch_copy:
                                extra = in_ch_target - ch_copy
                                filler = v.mean(dim=1, keepdim=True).expand(-1, extra, -1, -1)
                                new_w[:, ch_copy:] = filler
                        adapted[k] = new_w
                        print(f"[CLS] Adapted conv stem to {in_ch_target}ch from 3ch")
                        continue
                    except Exception:
                        pass
                # Otherwise skip mismatched tensors
                continue
            missing, unexpected = model.load_state_dict(adapted, strict=False)
            try:
                print(f"[CLS] Loaded local pretrained: {ckpt_path} (used={len(adapted)}, missing={len(missing)}, unexpected={len(unexpected)})")
            except Exception:
                pass
        else:
            try:
                print(f"[CLS] No local pretrained weights found; training from scratch.")
            except Exception:
                pass
    except Exception as e:
        try:
            print(f"[CLS] Local pretrained load failed: {e}")
        except Exception:
            pass
    model.to(dev)
    opt = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    total_epochs = int(getattr(args, "total_epochs", None) or args.epochs)
    sched = torch.optim.lr_scheduler.CosineAnnealingLR(opt, T_max=total_epochs)
    ce = nn.CrossEntropyLoss()

    best_acc = -1.0
    best_model_path_arg = (getattr(args, "best_model_path", None) or "").strip()
    best_onnl = best_model_path_arg or os.path.join(
        args.out, f"cls_{args.backbone}_best.onnlmodel"
    )
    best_pt = os.path.join(args.out, f"cls_{args.backbone}_best.pt")
    if best_model_path_arg:
        print(f"[CLS] best_model_path from request → {best_onnl}")
    else:
        print(f"[CLS] best_model_path not set; .onnlmodel will be saved to → {best_onnl}")
    ensure_dir(args.out)

    # Optional resume
    start_epoch = 1
    resume_from = str(getattr(args, "resume_from", "") or "")
    if resume_from and os.path.isfile(resume_from):
        try:
            ckpt = torch.load(resume_from, map_location="cpu")
            # Support .pt and .onnlmodel meta
            _meta = ckpt.get("meta") if isinstance(ckpt, dict) else None
            _backbone = (_meta or {}).get("backbone") if isinstance(_meta, dict) else ckpt.get("backbone")
            _in_chans = int(((_meta or {}).get("in_chans", in_ch)) if isinstance(_meta, dict) else ckpt.get("in_chans", in_ch))
            if _backbone and str(_backbone) != str(args.backbone):
                print(f"[CLS] Warning: resume backbone mismatch: {_backbone} != {args.backbone}")
            if _in_chans != in_ch:
                print(f"[CLS] Warning: resume in_chans mismatch: {_in_chans} != {in_ch}")
            # Model / opt / sched
            model.load_state_dict(ckpt["model"], strict=True)
            if "optimizer" in ckpt:
                try:
                    opt.load_state_dict(ckpt["optimizer"])  # type: ignore
                except Exception as e:
                    print(f"[CLS] optimizer resume failed: {e}")
            if "scheduler" in ckpt:
                try:
                    sched.load_state_dict(ckpt["scheduler"])  # type: ignore
                except Exception as e:
                    print(f"[CLS] scheduler resume failed: {e}")
            ep_meta = None
            if isinstance(_meta, dict):
                ep_meta = _meta.get("epoch")
                if _meta.get("best_acc") is not None:
                    try:
                        best_acc = float(_meta.get("best_acc"))
                    except Exception:
                        pass
            start_epoch = int((ckpt.get("epoch") if isinstance(ckpt, dict) else None) or (ep_meta or 0)) + 1
            print(f"[CLS] Resuming from {resume_from} at epoch {start_epoch}")
        except Exception as e:
            print(f"[CLS] Failed to resume from {resume_from}: {e}")

    should_stop = getattr(args, "stop_cb", None)
    # Early stopping controls
    patience = int(getattr(args, "patience", 0) or 0)
    min_delta = float(getattr(args, "min_delta", 0.0) or 0.0)
    epochs_no_improve = 0
    stop_requested = False
    for epoch in range(start_epoch, total_epochs+1):
        t0=time.time()
        # train
        model.train(); tr_loss=0.0
        tr_seen = 0
        for batch in tr_ld:
            if batch is None:
                continue
            x,y,_ = batch
            x=x.to(dev); y=y.to(dev)
            opt.zero_grad(set_to_none=True)
            logits = model(x)
            loss = ce(logits,y)
            loss.backward(); opt.step()
            tr_loss += loss.item()*x.size(0)
            tr_seen += x.size(0)
            # Stop check after batch
            if callable(should_stop) and should_stop():
                stop_requested = True
                break
        sched.step()

        # Removed 'last' checkpoint saving to emit only one model file per run

        # val (optional)
        if va_ld is not None:
            model.eval(); va_loss=0.0; correct=0; total=0
            va_seen = 0
            with torch.no_grad():
                for batch in va_ld:
                    if batch is None:
                        continue
                    x,y,names = batch
                    x=x.to(dev); y=y.to(dev)
                    logits = model(x)
                    loss = ce(logits,y)
                    va_loss += loss.item()*x.size(0)
                    va_seen += x.size(0)
                    pred = logits.argmax(1)
                    correct += (pred==y).sum().item()
                    total += y.numel()
            acc = correct/max(1,total)
            dt=time.time()-t0
            print(
                f"[{epoch:03d}] train {tr_loss/max(1,tr_seen):.4f} | val {va_loss/max(1,va_seen):.4f} "
                f"| acc {acc:.4f} | best {best_acc:.4f} | delta {acc - best_acc:+.4f} "
                f"| no_improve {epochs_no_improve}/{patience} | time {dt:.1f}s"
            )
            if acc > best_acc:
                best_acc = acc
                try:
                    meta_best = _build_onnl_meta_cls(args, class_names, in_ch, epoch, best_acc, True, source="best")
                    save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                    print(f"[CLS] Saved best .pt → {best_pt}")
                except Exception as e:
                    print(f"[CLS] Warning: failed to save best .pt: {e}")
                epochs_no_improve = 0
            else:
                # consider min_delta if set
                if (acc <= best_acc + min_delta):
                    epochs_no_improve += 1
                else:
                    epochs_no_improve = 0
                if patience > 0 and epochs_no_improve >= patience:
                    print(f"[CLS] Early stopping triggered (patience={patience}, best_acc={best_acc:.4f})")
                    try:
                        if not os.path.isfile(best_pt):
                            meta_best = _build_onnl_meta_cls(args, class_names, in_ch, epoch, best_acc, True, source="early_stop")
                            save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                        print("[CLS] Exporting best model to .onnlmodel (this may take a while)...")
                        pack_onnlmodel_from_pt(best_pt, best_onnl, opset=13)
                        print(f"[CLS] Export done → {best_onnl}")
                        if os.path.isfile(best_onnl) and os.path.isfile(best_pt):
                            os.remove(best_pt)
                            print(f"[CLS] Cleaned up intermediate .pt → {best_pt}")
                    except Exception as e:
                        print(f"[CLS] Warning: failed to export best .onnlmodel: {e}")
                    break
        else:
            dt=time.time()-t0
            print(f"[{epoch:03d}] train {tr_loss/max(1,tr_seen):.4f} | time {dt:.1f}s (no validation)")
            try:
                meta_best = _build_onnl_meta_cls(args, class_names, in_ch, epoch, best_acc, True, source="last")
                save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                print(f"[CLS] Saved latest .pt → {best_pt}")
            except Exception as e:
                print(f"[CLS] Warning: failed to save latest .pt: {e}")
        if stop_requested or (callable(should_stop) and should_stop()):
            print("[CLS] Stop requested — exporting last/best checkpoint before exit")
            try:
                if not os.path.isfile(best_pt):
                    meta_best = _build_onnl_meta_cls(args, class_names, in_ch, epoch, best_acc, True, source="stop_requested")
                    save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                print("[CLS] Exporting to .onnlmodel (this may take a while)...")
                pack_onnlmodel_from_pt(best_pt, best_onnl, opset=13)
                print(f"[CLS] Export done → {best_onnl}")
                if os.path.isfile(best_onnl) and os.path.isfile(best_pt):
                    os.remove(best_pt)
                    print(f"[CLS] Cleaned up intermediate .pt → {best_pt}")
            except Exception as e:
                print(f"[CLS] Warning: failed to export .onnlmodel on stop: {e}")
            break
    # Final packaging at normal end
    try:
        if not os.path.isfile(best_pt):
            meta_best = _build_onnl_meta_cls(args, class_names, in_ch, total_epochs, best_acc, True, source="final")
            save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
        print("[CLS] Final export to .onnlmodel (this may take a while)...")
        pack_onnlmodel_from_pt(best_pt, best_onnl, opset=13)
        print(f"[CLS] Export done → {best_onnl}")
        # Clean up intermediate .pt after successful .onnlmodel export
        if os.path.isfile(best_onnl) and os.path.isfile(best_pt):
            try:
                os.remove(best_pt)
                print(f"[CLS] Cleaned up intermediate .pt → {best_pt}")
            except Exception as _e:
                print(f"[CLS] Warning: failed to remove .pt: {_e}")
    except Exception as e:
        print(f"[CLS] Warning: failed to export final .onnlmodel: {e}")
    print(f"[Done] Best Acc={best_acc:.4f} | saved → {best_onnl}")


# -----------------------
# Fine-tune — Classification
# -----------------------
def finetune_cls(args):
    """Fine-tune an existing classification checkpoint on a new dataset.

    Requirements:
    - The dataset's class names must be a subset of the checkpoint class_names.
    - Order of outputs remains identical to the checkpoint's class_names.
    """
    set_seed(args.seed)
    dev = device_pick()
    print(f"[Device] {dev}")

    # Dataset construction (same as train_cls)
    img_dir = os.path.join(args.data, "raw")
    mask_dir = os.path.join(args.data, "seg")
    meta_dir = os.path.join(args.data, "meta")

    wafer_id = args.wafer_id
    if wafer_id < 0:
        wafer_id = infer_wafer_id([mp for _, mp in match_pairs(img_dir, mask_dir)])
        print(f"[Auto wafer-id] inferred wafer_id={wafer_id}")

    ds_all = ClsDataset(
        img_dir, mask_dir, meta_dir,
        img_size=args.img_size,
        wafer_id=wafer_id,
        min_defect_px=args.min_defect_px,
        aug=True,
        use_roi=getattr(args, "use_roi", False),
        roi_mode=getattr(args, "roi_mode", "none"),
        label_mode=getattr(args, "label_mode", "binary"),
        folders_only=bool(getattr(args, "folders_only", False)),
        add_fft=bool(getattr(args, "add_fft", False)),
    )
    if len(ds_all) < 1:
        raise RuntimeError("분류 데이터가 너무 적습니다(최소 1장 필요).")

    # Split
    idxs = list(range(len(ds_all)))
    random.shuffle(idxs)
    val_ratio = float(getattr(args, "val_split", 0.2))
    val_min = int(getattr(args, "val_min", 32))
    n_val = int(len(ds_all) * max(0.0, min(1.0, val_ratio)))
    n_val = max(n_val, val_min)
    n_val = min(n_val, len(ds_all))
    if val_ratio == 0.0:
        n_val = 0
    val_idx = idxs[:n_val]; tr_idx = idxs[n_val:]
    # Log split sizes (finetune)
    try:
        print(f"[Split-FT] train={len(tr_idx)} | val={len(val_idx)} | total={len(ds_all)}")
    except Exception:
        pass
    if len(tr_idx) == 0:
        raise RuntimeError("학습 샘플이 없습니다. val_split/val_min을 조정하세요.")

    # Load checkpoint and set class mapping
    ckpt = torch.load(args.weights, map_location="cpu")
    backbone = ckpt["backbone"]
    class_names_ckpt = list(ckpt["class_names"])  # defines output dim and order
    n_classes = len(class_names_ckpt)
    print("[CKPT Classes]", class_names_ckpt)

    # Verify dataset labels are all present in checkpoint classes
    cls2id_ckpt = {c: i for i, c in enumerate(class_names_ckpt)}
    for cname in ds_all.class_names:
        if cname not in cls2id_ckpt:
            raise RuntimeError(f"Dataset class '{cname}' not present in checkpoint class_names.")

    # Configure augmentation balancing similar to train_cls (based on dataset distribution)
    labels_tr_local = [ds_all.cls2id[ds_all.items[i][1]] for i in tr_idx]
    class_sample_count_local = np.bincount(labels_tr_local, minlength=len(ds_all.class_names))
    nz_local = class_sample_count_local > 0
    median_cnt_local = int(np.median(class_sample_count_local[nz_local])) if nz_local.any() else 0
    ds_all.minority_classes = set([ds_all.class_names[c] for c in range(len(ds_all.class_names)) if class_sample_count_local[c] < median_cnt_local])
    ds_all.use_minority_strong_aug = bool(getattr(args, "balance_aug", False))

    # DataLoaders
    tr_ds = Subset(ds_all, tr_idx)
    va_ds = Subset(ds_all, val_idx) if n_val > 0 else None

    pin = (dev.type == "cuda")
    if dev.type == "mps": pin = False
    if bool(getattr(args, "balance_sampler", True)):
        # Balance by dataset label distribution (local indices)
        inv = np.zeros_like(class_sample_count_local, dtype=np.float64)
        inv[nz_local] = class_sample_count_local[nz_local].max() / class_sample_count_local[nz_local]
        samples_weight = np.array([inv[l] for l in labels_tr_local])
        sampler = WeightedRandomSampler(torch.from_numpy(samples_weight).double(), num_samples=len(samples_weight), replacement=True)
        tr_ld = DataLoader(tr_ds, batch_size=args.batch_size, sampler=sampler, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)
    else:
        tr_ld = DataLoader(tr_ds, batch_size=args.batch_size, shuffle=True, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)
    va_ld = None if va_ds is None else DataLoader(va_ds, batch_size=max(1, args.batch_size//2), shuffle=False, num_workers=args.workers, pin_memory=pin, collate_fn=collate_skip_none)

    # Model from checkpoint
    in_ch = int(ckpt.get("in_chans", 3))
    model = timm.create_model(backbone, pretrained=False, num_classes=n_classes, in_chans=in_ch)
    model.load_state_dict(ckpt["model"], strict=True)
    model.to(dev)

    opt = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    total_epochs = int(getattr(args, "total_epochs", None) or args.epochs)
    sched = torch.optim.lr_scheduler.CosineAnnealingLR(opt, T_max=total_epochs)
    ce = nn.CrossEntropyLoss()

    best_acc = -1.0
    ensure_dir(args.out)
    best_onnl = (getattr(args, "best_model_path", None) or "").strip() or os.path.join(
        args.out, f"cls_finetune_{backbone}_best.onnlmodel"
    )
    best_pt = os.path.join(args.out, f"cls_finetune_{backbone}_best.pt")

    def map_to_ckpt_indices(y_tensor: torch.Tensor) -> torch.Tensor:
        # y_tensor contains indices in ds_all.class_names space
        y_list = y_tensor.detach().cpu().numpy().tolist()
        mapped = [int(cls2id_ckpt[ds_all.class_names[int(v)]]) for v in y_list]
        return torch.tensor(mapped, dtype=torch.long, device=dev)

    should_stop = getattr(args, "stop_cb", None)
    patience = int(getattr(args, "patience", 0) or 0)
    min_delta = float(getattr(args, "min_delta", 0.0) or 0.0)
    epochs_no_improve = 0
    stop_requested = False
    for epoch in range(1, total_epochs+1):
        t0 = time.time()
        # train
        model.train(); tr_loss = 0.0
        tr_seen = 0
        for batch in tr_ld:
            if batch is None:
                continue
            x, y, _ = batch
            x = x.to(dev)
            y_ckpt = map_to_ckpt_indices(y)
            opt.zero_grad(set_to_none=True)
            logits = model(x)
            loss = ce(logits, y_ckpt)
            loss.backward(); opt.step()
            tr_loss += loss.item() * x.size(0)
            tr_seen += x.size(0)
            if callable(should_stop) and should_stop():
                stop_requested = True
                break
        sched.step()

        # Removed 'last' checkpoint saving to emit only one model file per run

        if va_ld is not None:
            model.eval(); va_loss = 0.0; correct = 0; total = 0
            va_seen = 0
            with torch.no_grad():
                for batch in va_ld:
                    if batch is None:
                        continue
                    x, y, _ = batch
                    x = x.to(dev)
                    y_ckpt = map_to_ckpt_indices(y)
                    logits = model(x)
                    loss = ce(logits, y_ckpt)
                    va_loss += loss.item() * x.size(0)
                    va_seen += x.size(0)
                    pred = logits.argmax(1)
                    correct += (pred == y_ckpt).sum().item()
                    total += y_ckpt.numel()
            acc = correct / max(1, total)
            dt = time.time() - t0
            print(
                f"[FT {epoch:03d}] train {tr_loss/max(1,tr_seen):.4f} | val {va_loss/max(1,va_seen):.4f} "
                f"| acc {acc:.4f} | best {best_acc:.4f} | delta {acc - best_acc:+.4f} "
                f"| no_improve {epochs_no_improve}/{patience} | time {dt:.1f}s"
            )
            if acc > best_acc:
                best_acc = acc
                try:
                    meta_best = _build_onnl_meta_cls(args, class_names_ckpt, in_ch, epoch, best_acc, True, source="best_ft")
                    save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                    print(f"[CLS-FT] Saved best .pt → {best_pt}")
                except Exception as e:
                    print(f"[CLS-FT] Warning: failed to save best .pt: {e}")
                epochs_no_improve = 0
            else:
                if (acc <= best_acc + min_delta):
                    epochs_no_improve += 1
                else:
                    epochs_no_improve = 0
                if patience > 0 and epochs_no_improve >= patience:
                    print(f"[CLS-FT] Early stopping triggered (patience={patience}, best_acc={best_acc:.4f})")
                    break
        else:
            dt = time.time() - t0
            print(f"[FT {epoch:03d}] train {tr_loss/max(1,tr_seen):.4f} | time {dt:.1f}s (no validation)")
            try:
                meta_best = _build_onnl_meta_cls(args, class_names_ckpt, in_ch, epoch, best_acc, True, source="last_ft")
                save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                print(f"[CLS-FT] Saved latest .pt → {best_pt}")
            except Exception as e:
                print(f"[CLS-FT] Warning: failed to save latest .pt: {e}")

        if stop_requested or (callable(should_stop) and should_stop()):
            print("[CLS-FT] Stop requested — exporting last/best checkpoint before exit")
            try:
                if not os.path.isfile(best_pt):
                    meta_best = _build_onnl_meta_cls(args, class_names_ckpt, in_ch, epoch, best_acc, True, source="stop_requested_ft")
                    save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
                print("[CLS-FT] Exporting to .onnlmodel (this may take a while)...")
                pack_onnlmodel_from_pt(best_pt, best_onnl, opset=13)
                print(f"[CLS-FT] Export done → {best_onnl}")
                if os.path.isfile(best_onnl) and os.path.isfile(best_pt):
                    os.remove(best_pt)
                    print(f"[CLS-FT] Cleaned up intermediate .pt → {best_pt}")
            except Exception as e:
                print(f"[CLS-FT] Warning: failed to export .onnlmodel on stop: {e}")
            break

    # Final packaging at normal end
    try:
        if not os.path.isfile(best_pt):
            meta_best = _build_onnl_meta_cls(args, class_names_ckpt, in_ch, total_epochs, best_acc, True, source="final_ft")
            save_onnlmodel_cls(best_pt, model, meta_best, optimizer=opt, scheduler=sched)
        print("[CLS-FT] Final export to .onnlmodel (this may take a while)...")
        pack_onnlmodel_from_pt(best_pt, best_onnl, opset=13)
        print(f"[Done FT] Export done → {best_onnl}")
        # Clean up intermediate .pt after successful .onnlmodel export
        if os.path.isfile(best_onnl) and os.path.isfile(best_pt):
            try:
                os.remove(best_pt)
                print(f"[CLS-FT] Cleaned up intermediate .pt → {best_pt}")
            except Exception as _e:
                print(f"[CLS-FT] Warning: failed to remove .pt: {_e}")
    except Exception as e:
        print(f"[CLS-FT] Warning: failed to export final .onnlmodel: {e}")

# -----------------------
# Evaluation — Classification
# -----------------------
def _compute_metrics_from_cm(cm: np.ndarray, class_names: List[str]) -> Dict[str, object]:
    # cm shape: (C,C), rows=true, cols=pred
    eps = 1e-9
    per_class = {}
    total_correct = int(np.trace(cm))
    total = int(cm.sum())
    overall_acc = float(total_correct / max(1, total))
    for i, name in enumerate(class_names):
        tp = float(cm[i, i])
        fp = float(cm[:, i].sum() - tp)
        fn = float(cm[i, :].sum() - tp)
        prec = tp / max(eps, tp + fp)
        rec = tp / max(eps, tp + fn)
        f1 = 2 * prec * rec / max(eps, prec + rec)
        support = int(cm[i, :].sum())
        per_class[name] = {"precision":prec, "recall":rec, "f1":f1, "support":support}
    return {"overall_acc": overall_acc, "per_class": per_class, "total": total}


def _plot_confusion_matrix(cm: np.ndarray, class_names: List[str], out_path: str, title: str = "Confusion Matrix"):
    fig, ax = plt.subplots(figsize=(max(6, len(class_names)*0.8), max(5, len(class_names)*0.6)))
    im = ax.imshow(cm, interpolation='nearest', cmap=plt.cm.Blues)
    ax.figure.colorbar(im, ax=ax)
    ax.set(xticks=np.arange(cm.shape[1]), yticks=np.arange(cm.shape[0]),
           xticklabels=class_names, yticklabels=class_names,
           ylabel='True label', xlabel='Predicted label', title=title)
    plt.setp(ax.get_xticklabels(), rotation=45, ha="right", rotation_mode="anchor")
    thresh = cm.max() / 2.0 if cm.size > 0 else 0.5
    for i in range(cm.shape[0]):
        for j in range(cm.shape[1]):
            ax.text(j, i, format(int(cm[i, j])), ha="center", va="center",
                    color="white" if cm[i, j] > thresh else "black")
    fig.tight_layout()
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    fig.savefig(out_path, dpi=150)
    plt.close(fig)


def eval_cls(weights: str, data: str, img_size: int = 512, wafer_id: int = -1,
             min_defect_px: int = 32, use_roi: bool = False, roi_mode: str = "none",
             out_dir: str = "runs_cls/eval", label_mode: str = "category") -> Dict[str, object]:
    """Evaluate classification model checkpoint on dataset constructed like ClsDataset.
    Writes confusion matrix image and returns JSON-serializable metrics.
    """
    use_ort = False
    ort_sess = None
    input_name = None
    # Load metadata and model handle
    if weights.lower().endswith('.onnlmodel'):
        import zipfile as _zip
        import json as _json
        import onnxruntime as ort  # type: ignore
        with _zip.ZipFile(weights, 'r') as zf:
            # Load meta.json
            meta_bytes = None
            onnx_name = None
            for e in zf.infolist():
                if e.filename.lower().endswith('meta.json') or e.filename.lower() == 'meta.json':
                    meta_bytes = zf.read(e.filename)
                if e.filename.lower().endswith('.onnx'):
                    onnx_name = e.filename
            meta = {}
            if meta_bytes:
                try:
                    meta = _json.loads(meta_bytes.decode('utf-8'))
                except Exception:
                    meta = {}
            class_names_ckpt = list(meta.get('class_names', [])) if isinstance(meta, dict) else []
            n_classes = len(class_names_ckpt)
            # ORT session (handle in-memory or temp file fallback)
            if onnx_name is None:
                raise RuntimeError(".onnlmodel does not contain model.onnx")
            onnx_bytes = zf.read(onnx_name)
            try:
                ort_sess = ort.InferenceSession(onnx_bytes, providers=["CPUExecutionProvider"])  # type: ignore[arg-type]
            except Exception:
                import tempfile as _tmp, os as _os
                tmp = None
                try:
                    with _tmp.NamedTemporaryFile(delete=False, suffix='.onnx') as f:
                        f.write(onnx_bytes)
                        tmp = f.name
                    ort_sess = ort.InferenceSession(tmp, providers=["CPUExecutionProvider"])  # type: ignore[arg-type]
                finally:
                    if tmp and _os.path.exists(tmp):
                        try:
                            _os.remove(tmp)
                        except Exception:
                            pass
            use_ort = True
            input_name = ort_sess.get_inputs()[0].name
            try:
                ishape = ort_sess.get_inputs()[0].shape
                in_ch = int(ishape[1]) if len(ishape) >= 2 and isinstance(ishape[1], int) else 3
            except Exception:
                in_ch = 3
    else:
        ckpt = torch.load(weights, map_location="cpu")
        # Support both legacy .pt and dict-onpt
        if isinstance(ckpt, dict) and "meta" in ckpt and isinstance(ckpt.get("meta"), dict):
            meta = ckpt["meta"]
            backbone = str(meta.get("backbone"))
            class_names_ckpt = list(meta.get("class_names", []))
            in_ch = int(meta.get("in_chans", 3))
        else:
            backbone = ckpt["backbone"]
            class_names_ckpt = list(ckpt["class_names"])  # ordering defines output dim
            in_ch = int(ckpt.get("in_chans", 3))
        n_classes = len(class_names_ckpt)

    dev = device_pick()

    img_dir = os.path.join(data, "raw")
    mask_dir = os.path.join(data, "seg")
    meta_dir = os.path.join(data, "meta")

    if wafer_id < 0:
        wafer_id = infer_wafer_id([mp for _, mp in match_pairs(img_dir, mask_dir)])

    # Auto-detect folder-based dataset if raw/seg are missing
    folders_only = False
    cls_raw = os.path.join(data, "cls", "raw")
    if not (os.path.isdir(img_dir) and os.path.isdir(mask_dir)):
        if os.path.isdir(cls_raw):
            folders_only = True
        else:
            # Detect if data/ contains <CATEGORY> subfolders with images
            try:
                for cat in os.listdir(data):
                    cp = os.path.join(data, cat)
                    if not os.path.isdir(cp):
                        continue
                    for fn in os.listdir(cp):
                        if fn.lower().endswith((".png",".jpg",".jpeg",".bmp",".tif",".tiff")):
                            folders_only = True
                            raise StopIteration
            except StopIteration:
                pass

    ds = ClsDataset(
        img_dir, mask_dir, meta_dir,
        img_size=img_size,
        wafer_id=wafer_id,
        min_defect_px=min_defect_px,
        aug=False,
        use_roi=use_roi,
        roi_mode=roi_mode,
        label_mode=str(label_mode),
        folders_only=folders_only,
        add_fft=bool(int(in_ch) in (2, 4)),
        gray_input=bool(int(in_ch) in (1, 2)),
    )

    # Map dataset labels to checkpoint label indices by name
    cls2id_ckpt = {c:i for i,c in enumerate(class_names_ckpt)}
    # Ensure all ds classes exist in ckpt classes
    for cname in ds.class_names:
        if cname not in cls2id_ckpt:
            raise RuntimeError(f"Class '{cname}' in dataset not present in checkpoint class_names.")

    pin = (dev.type == "cuda")
    if dev.type == "mps": pin = False
    ld = DataLoader(ds, batch_size=32, shuffle=False, num_workers=2, pin_memory=pin)

    if not use_ort:
        model = timm.create_model(backbone, pretrained=False, num_classes=n_classes, in_chans=in_ch)
        model.load_state_dict(ckpt["model"], strict=True)
        model.to(dev).eval()

    y_true = []
    y_pred = []
    with torch.no_grad():
        for x, y, _ in ld:
            if use_ort and ort_sess is not None and input_name is not None:
                xb_np = x.cpu().numpy()
                logits = ort_sess.run(None, {input_name: xb_np})[0]
                pred = logits.argmax(1).tolist()
            else:
                x = x.to(dev)
                logits = model(x)
                pred = logits.argmax(1).cpu().numpy().tolist()
            # map ds y to ckpt y by name
            for yi in y.numpy().tolist():
                cname = ds.class_names[yi]
                y_true.append(int(cls2id_ckpt[cname]))
            y_pred.extend([int(p) for p in pred])

    y_true = np.array(y_true, dtype=np.int64)
    y_pred = np.array(y_pred, dtype=np.int64)
    cm = np.zeros((n_classes, n_classes), dtype=np.int64)
    for t, p in zip(y_true, y_pred):
        if 0 <= t < n_classes and 0 <= p < n_classes:
            cm[t, p] += 1

    metrics = _compute_metrics_from_cm(cm, class_names_ckpt)
    os.makedirs(out_dir, exist_ok=True)
    cm_path = os.path.join(out_dir, "confusion_matrix.png")
    _plot_confusion_matrix(cm, class_names_ckpt, cm_path, title="Classification Confusion Matrix")
    metrics["report_image"] = cm_path
    metrics["classes"] = class_names_ckpt
    return metrics

# -----------------------
# Inference helper (Seg)
# -----------------------
def infer_seg(weights:str, data_dir:str, img_size:int=768, out_dir:str="inference_out"):
    ckpt = torch.load(weights, map_location="cpu")
    arch=ckpt["arch"]; enc=ckpt["encoder"]; ncls=ckpt["num_classes"]
    three=ckpt.get("three_classes", False); wafer_id=ckpt.get("wafer_id",1)
    _require_smp()
    if arch=="deeplabv3plus":
        model = smp.DeepLabV3Plus(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    elif arch=="unet":
        model = smp.Unet(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    elif arch=="unetplusplus":
        model = smp.UnetPlusPlus(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    elif arch=="fpn":
        model = smp.FPN(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    else:
        raise ValueError(arch)
    model.load_state_dict(ckpt["model"], strict=True)

    dev = device_pick(); model.to(dev).eval()
    ensure_dir(out_dir)

    img_dir = os.path.join(data_dir, "raw")
    mask_dir= os.path.join(data_dir, "seg")
    pairs = match_pairs(img_dir, mask_dir)

    tf = A.Compose([
        A.LongestMaxSize(max_size=img_size),
        A.PadIfNeeded(min_height=img_size, min_width=img_size, border_mode=cv2.BORDER_REFLECT_101),
        A.CenterCrop(img_size, img_size),
        A.Normalize(),
        ToTensorV2(),
    ])

    with torch.no_grad():
        for i, (ip, mp) in enumerate(pairs[:20]):  # 샘플 20장
            rgb = imread_rgb(ip)
            x = tf(image=rgb)["image"].unsqueeze(0).to(dev)
            pred = torch.argmax(model(x), dim=1)[0].cpu().numpy()
            stem = os.path.splitext(os.path.basename(ip))[0]
            out_base = os.path.join(out_dir, f"{stem}")
            class_names = ["bg","wafer","defect"] if three else None
            save_mask_and_overlay(rgb, pred, out_base, class_names=class_names)
    print(f"[Infer] wrote overlays to {out_dir}")


def infer_seg_slide(weights:str, img_path:str, out_path:str, tile:int=1024, overlap:int=256):
    ckpt = torch.load(weights, map_location="cpu")
    arch=ckpt["arch"]; enc=ckpt["encoder"]; ncls=ckpt["num_classes"]
    _require_smp()
    if arch=="deeplabv3plus":
        model = smp.DeepLabV3Plus(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    elif arch=="unet":
        model = smp.Unet(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    elif arch=="unetplusplus":
        model = smp.UnetPlusPlus(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    elif arch=="fpn":
        model = smp.FPN(encoder_name=enc, encoder_weights=None, classes=ncls, activation=None)
    else:
        raise ValueError(arch)
    model.load_state_dict(ckpt["model"], strict=True)
    dev = device_pick(); model.to(dev).eval()

    rgb = imread_rgb(img_path); H, W = rgb.shape[:2]
    stride = max(1, tile - overlap)
    prob_sum = np.zeros((ncls, H, W), np.float32)
    count    = np.zeros((H, W), np.float32)

    tf = A.Compose([A.Normalize(), ToTensorV2()])
    with torch.no_grad():
        for y0 in range(0, max(1, H-tile+1), stride):
            for x0 in range(0, max(1, W-tile+1), stride):
                y1 = min(H, y0+tile); x1 = min(W, x0+tile)
                patch = rgb[y0:y1, x0:x1]
                if patch.shape[0]!=tile or patch.shape[1]!=tile:
                    pad = np.zeros((tile, tile, 3), np.uint8)
                    pad[:patch.shape[0], :patch.shape[1]] = patch
                    patch = pad
                x = tf(image=patch)["image"].unsqueeze(0).to(dev)
                logits = model(x)[0].detach().cpu().numpy()
                ph, pw = y1-y0, x1-x0
                prob_sum[:, y0:y1, x0:x1] += logits[:, :ph, :pw]
                count[y0:y1, x0:x1] += 1.0
    prob = prob_sum / np.clip(count, 1e-6, None)
    pred = prob.argmax(0).astype(np.uint8)
    save_overlay(rgb, pred, out_path)


# -----------------------
# Main / CLI
# -----------------------
def build_parser():
    p = argparse.ArgumentParser()
    sub = p.add_subparsers(dest="mode", required=True)

    # Seg
    ps = sub.add_parser("seg", help="Semantic segmentation training")
    ps.add_argument("--data", required=True, help="dataset dir (contains raw/ seg/ meta/)")
    ps.add_argument("--out", default="runs_seg")
    ps.add_argument("--epochs", type=int, default=50)
    ps.add_argument("--batch-size", type=int, default=4)
    ps.add_argument("--workers", type=int, default=4)
    ps.add_argument("--img-size", type=int, default=768)
    ps.add_argument("--arch", default="deeplabv3plus", choices=["deeplabv3plus","unet","unetplusplus","fpn"])
    ps.add_argument("--encoder", default="resnet50")  # e.g., resnet50, timm-efficientnet-b0, mit_b2, etc.
    ps.add_argument("--lr", type=float, default=3e-4)
    ps.add_argument("--dice-weight", type=float, default=0.3)
    ps.add_argument("--class-weights", action="store_true")
    ps.add_argument("--ce-weight-cap", type=float, default=10.0, help="CE 클래스 가중치 상한")
    ps.add_argument("--ignore-index", type=int, default=-1)
    ps.add_argument("--three-classes", action="store_true", help="collapse to {0:bg,1:wafer,2:defect}")
    ps.add_argument("--wafer-id", type=int, default=-1, help="wafer class id in original masks (negative to auto-infer)")
    ps.add_argument("--pos-crop", type=float, default=0.0, help="확률 p로 결함 픽셀을 반드시 포함하는 크롭을 샘플링")
    ps.add_argument("--min-defect-px-in-tile", type=int, default=24)
    ps.add_argument("--boundary-ignore-px", type=int, default=0)
    ps.add_argument("--rand-thicken-p", type=float, default=0.0)
    ps.add_argument("--thicken-radius", type=int, default=1)
    ps.add_argument("--seed", type=int, default=42)
    ps.add_argument("--amp", action="store_true")
    # New: input/augmentation/imbalance controls
    ps.add_argument("--input-mode", default="rgb", choices=["rgb","kgray","kgray_precomputed"],
                   help="rgb: use original RGB; kgray: convert RGB to K-gray on the fly; kgray_precomputed: tiles already saved as 3ch K-gray")
    ps.add_argument("--strong-defect-aug", action="store_true", help="apply stronger augs to tiles containing defects")
    ps.add_argument("--defect-oversample", type=float, default=1.0, help=">1 to oversample tiles with defects (e.g., 3.0)")

    # Cls
    pc = sub.add_parser("cls", help="Image-level classification training")
    pc.add_argument("--data", required=True)
    pc.add_argument("--out", default="runs_cls")
    pc.add_argument("--epochs", type=int, default=20)
    pc.add_argument("--batch-size", type=int, default=16)
    pc.add_argument("--workers", type=int, default=4)
    pc.add_argument("--img-size", type=int, default=512)
    pc.add_argument("--backbone", default="tf_efficientnet_b0")
    pc.add_argument("--lr", type=float, default=3e-4)
    pc.add_argument("--wafer-id", type=int, default=-1)
    pc.add_argument("--min-defect-px", type=int, default=32)
    pc.add_argument("--seed", type=int, default=42)
    pc.add_argument("--use-roi", action="store_true", help="Apply ROI before transforms")
    pc.add_argument("--roi-mode", default="none", choices=["none","mask","auto"], help="ROI source: mask=from seg, auto=heuristic")
    pc.add_argument("--label-mode", default="binary", choices=["binary","category"], help="binary=OK/DEFECT, category=use meta category if available")
    pc.add_argument("--folders-only", action="store_true", help="Use only folder-based labels under data/cls/raw/<CATEGORY> instead of mask/meta")
    pc.add_argument("--balance-sampler", type=int, default=1, help="1 to enable class-balancing sampler, 0 to disable")
    pc.add_argument("--balance-aug", type=int, default=1, help="1 to apply stronger augs to minority classes, 0 to disable")
    pc.add_argument("--add-fft", action="store_true", help="Append 1ch FFT magnitude to RGB for 4-channel input")
    pc.add_argument("--patience", type=int, default=0, help="Early stop if no val acc improvement for N epochs (0=off)")
    pc.add_argument("--min-delta", type=float, default=0.0, help="Minimum accuracy improvement to reset patience")
    return p

if __name__ == "__main__":
    # Support PyInstaller-frozen multiprocessing on Windows (e.g., DataLoader workers)
    try:
        import multiprocessing as _mp
        _mp.freeze_support()
    except Exception:
        pass
    # If launched as a multiprocessing child under PyInstaller, argv may contain 'parent_pid=...'.
    # In that case, delegate to spawn_main and exit before normal CLI parsing.
    try:
        import sys as _sys
        if any((str(a).startswith("parent_pid=") for a in _sys.argv[1:])):
            from multiprocessing.spawn import spawn_main as _spawn_main
            _spawn_main()
            raise SystemExit(0)
    except Exception:
        pass
    args = build_parser().parse_args()
    os.makedirs(args.out, exist_ok=True)
    if args.mode == "seg":
        train_seg(args)
        # quick inference sample (same data)
        best = sorted(glob(os.path.join(args.out, "seg_*_best.pt")))
        if best:
            infer_seg(best[-1], args.data, img_size=args.img_size, out_dir=os.path.join(args.out, "infer_samples"))
    elif args.mode == "cls":
        train_cls(args)


