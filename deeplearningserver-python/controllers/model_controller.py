from pathlib import Path
import shutil

import httpx
from fastapi import APIRouter, File, Form, HTTPException, UploadFile

from config import settings
from dtos import CopyFileRequest, MigrationDto, SendRemoteRequest

router = APIRouter()


def _format_file_size(num_bytes: int) -> str:
    size = float(num_bytes)
    for suffix in ["B", "KB", "MB", "GB", "TB"]:
        if size < 1024:
            return f"{size:.1f} {suffix}"
        size /= 1024
    return f"{size:.1f} PB"


def _resolve_path(path_value: str) -> Path:
    p = Path(path_value)
    if p.is_absolute():
        return p
    return Path(settings.model_directory) / path_value


@router.get("/list", response_model=dict)
async def get_models(size: str | None = None):
    valid_sizes = {"LARGE", "MIDDLE"}
    if size and size.upper() not in valid_sizes:
        raise HTTPException(status_code=400, detail="Invalid size parameter")

    base_model_dir = Path(settings.model_directory)
    if not base_model_dir.exists():
        return {"message": "Models retrieved successfully", "count": 0, "filters": {"size": size}, "models": []}

    search_sizes = [size.upper()] if size else sorted(valid_sizes)
    models = []
    known_types = {"BASE", "Release", "EVALUATION"}
    for search_size in search_sizes:
        search_path = base_model_dir / search_size
        if not search_path.exists():
            continue
        for model_file in list(search_path.rglob("*.edltool")) + list(search_path.rglob("*.onnlmodel")):
            stat = model_file.stat()
            relative_path = model_file.relative_to(base_model_dir).as_posix()
            parts = relative_path.split("/")
            inferred_type = None
            inferred_adms_name = None
            if len(parts) >= 3 and parts[1] in known_types:
                inferred_type = parts[1]
                inferred_adms_name = parts[2]
            elif len(parts) >= 2:
                inferred_adms_name = parts[1]
            models.append(
                {
                    "file_name": model_file.name,
                    "full_path": str(model_file),
                    "relative_path": relative_path,
                    "size": search_size,
                    "type": inferred_type,
                    "adms_name": inferred_adms_name,
                    "process_id": model_file.stem,
                    "file_size_bytes": stat.st_size,
                    "file_size_formatted": _format_file_size(stat.st_size),
                    "created_date": model_file.stat().st_ctime,
                    "modified_date": model_file.stat().st_mtime,
                }
            )

    models.sort(key=lambda m: (m.get("size"), m.get("type") or "", m.get("adms_name") or "", m.get("process_id")))
    return {
        "message": "Models retrieved successfully",
        "count": len(models),
        "filters": {"size": size},
        "models": models,
    }


@router.post("/upload", response_model=dict)
async def upload_model(model_path: str = Form(...), file: UploadFile = File(...)):
    target_path = _resolve_path(model_path)
    target_path.parent.mkdir(parents=True, exist_ok=True)
    content = await file.read()
    target_path.write_bytes(content)
    return {"message": "Model upload complete.", "file_path": str(target_path)}


@router.post("/migrate", response_model=dict)
async def migrate_models(model_migrations: MigrationDto):
    return {
        "message": "Model migration is not supported in Python mode (Euresys dependency).",
        "status": "not_supported",
        "old_models_path": model_migrations.old_models_path,
        "new_models_path": model_migrations.new_models_path,
    }


@router.post("/copy-file", response_model=dict)
async def copy_file(request: CopyFileRequest):
    source_path = _resolve_path(request.source_path)
    destination_path = _resolve_path(request.destination_path)
    if not source_path.exists():
        raise HTTPException(status_code=404, detail="Source file not found")
    destination_path.parent.mkdir(parents=True, exist_ok=True)
    if request.move:
        shutil.move(str(source_path), str(destination_path))
        op = "MOVE"
        message = "File moved successfully."
    else:
        shutil.copy2(source_path, destination_path)
        op = "COPY"
        message = "File copied successfully."
    return {
        "message": message,
        "operation": op,
        "source_path": str(source_path),
        "destination_path": str(destination_path),
        "file_size_bytes": destination_path.stat().st_size,
    }


@router.post("/send-remote", response_model=dict)
async def send_to_remote(request: SendRemoteRequest):
    local_path = _resolve_path(request.local_path)
    if not local_path.exists():
        raise HTTPException(status_code=404, detail="Local file not found")

    api_url = f"http://{request.remote_ip}/api/model/upload"
    files = {"file": (local_path.name, local_path.read_bytes(), "application/octet-stream")}
    data = {"model_path": request.remote_destination_path}
    async with httpx.AsyncClient(timeout=60.0) as client:
        response = await client.post(api_url, files=files, data=data)

    if response.is_success:
        return {
            "message": "Remote upload succeeded.",
            "remote_server": request.remote_ip,
            "local_path": str(local_path),
            "remote_destination_path": request.remote_destination_path,
            "remote_status_code": response.status_code,
            "remote_response": response.text,
        }
    raise HTTPException(
        status_code=502,
        detail={
            "message": "Remote upload failed.",
            "remote_status_code": response.status_code,
            "remote_response": response.text,
        },
    )