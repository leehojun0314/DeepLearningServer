from __future__ import annotations

import asyncio
import shutil
from pathlib import Path
from typing import Awaitable, Callable

import httpx

from dtos import (
    PyCategorySource,
    PyClassifyResponse,
    PyClassificationResult,
    PyClsTrainRequest,
    PyConfusionMatrixResponse,
    PyConfusionResponse,
    PyOnnlPackRequest,
    PyOnnlPackResponse,
    PyTrainingParameters,
    PyTrainLogResponse,
    PyTrainStartResponse,
    PyTrainStatusResponse,
    PyTrainStopResponse,
)


TrainCallback = Callable[[bool, float, int, float, float, float, float], Awaitable[None]]


class TrainingAiHttpBridge:
    current_instance: "TrainingAiHttpBridge | None" = None

    @classmethod
    def set_current_instance(cls, instance: "TrainingAiHttpBridge | None") -> None:
        cls.current_instance = instance

    def __init__(self, base_url: str = "http://localhost:8000", poll_interval: float = 1.0):
        self._base_url = base_url.rstrip("/")
        self._poll_interval = poll_interval
        self._parameters: PyTrainingParameters | None = None
        self._last_request: PyClsTrainRequest | None = None
        self._last_status: PyTrainStatusResponse | None = None
        self._data_path: str | None = None
        self._out_path: str | None = None
        self._best_model_path: str | None = None
        self._categories: list[str] = []
        self._category_sources: list[PyCategorySource] = []
        self._temp_image_session_dir: str | None = None
        self._is_training = False
        self._cancel_event = asyncio.Event()
        self._client = httpx.AsyncClient(timeout=300.0)

    def set_parameters(self, param: PyTrainingParameters) -> None:
        self._parameters = param

    def set_paths(self, data_path: str, out_path: str, best_model_path: str | None = None) -> None:
        self._data_path = data_path
        self._out_path = out_path
        self._best_model_path = best_model_path

    def set_categories(self, categories: list[str]) -> None:
        self._categories = categories

    @staticmethod
    def _enumerate_image_files(path: Path) -> list[Path]:
        files: list[Path] = []
        for ext in ("*.jpg", "*.png"):
            files.extend(path.rglob(ext))
        return files

    @staticmethod
    def _resolve_status_from_path(path: str) -> str:
        upper = path.upper().replace("\\", "/")
        if "/BASE/" in upper:
            return "Base"
        if "/NEW/" in upper:
            return "New"
        return "Unknown"

    def _copy_category_images_to_temp(self, source_paths: list[str], temp_target_root: Path) -> list[str]:
        temp_target_root.mkdir(parents=True, exist_ok=True)
        copied_paths: list[str] = []
        for source in source_paths:
            source_path = Path(source)
            if not source_path.exists():
                continue
            parent_name = source_path.parent.name
            leaf_name = source_path.name
            destination = temp_target_root / parent_name / leaf_name
            destination.mkdir(parents=True, exist_ok=True)
            for image_file in self._enumerate_image_files(source_path):
                rel = image_file.relative_to(source_path)
                target_file = destination / rel
                target_file.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(image_file, target_file)
            copied_paths.append(str(destination))
        return copied_paths

    async def load_images(
        self,
        categories: list[str],
        process_names: list[str],
        image_path: str,
        temp_image_dir: str | None = None,
    ) -> int:
        self._categories = categories
        self._data_path = image_path
        self._category_sources = []

        use_temp_copy = bool(temp_image_dir)
        if use_temp_copy:
            session_dir = Path(temp_image_dir) / Path().cwd().name / "tmp_training_images"
            session_dir.mkdir(parents=True, exist_ok=True)
            self._temp_image_session_dir = str(session_dir)
        else:
            self._temp_image_session_dir = None

        count = 0

        for category in categories:
            paths: list[str] = []
            for status in ("BASE", "NEW"):
                p = Path(image_path) / "NG" / status / category.upper()
                if p.exists():
                    paths.append(str(p))
            if not paths:
                continue
            target_paths = paths
            if use_temp_copy and self._temp_image_session_dir:
                target_paths = self._copy_category_images_to_temp(
                    paths, Path(self._temp_image_session_dir) / "NG" / category.upper()
                )
            self._category_sources.append(PyCategorySource(label=category.upper(), paths=target_paths))
            for path in target_paths:
                count += len(self._enumerate_image_files(Path(path)))

        ok_paths: list[str] = []
        for process_name in process_names:
            for status in ("BASE", "NEW"):
                p = Path(image_path) / "OK" / process_name / status
                if p.exists():
                    ok_paths.append(str(p))
        if ok_paths:
            target_ok_paths = ok_paths
            if use_temp_copy and self._temp_image_session_dir:
                target_ok_paths = self._copy_category_images_to_temp(
                    ok_paths, Path(self._temp_image_session_dir) / "OK"
                )
            self._category_sources.append(PyCategorySource(label="OK", paths=target_ok_paths))
            for path in target_ok_paths:
                count += len(self._enumerate_image_files(Path(path)))

        return count

    def get_training_image_records(
        self, process_adms_mapping: dict[str, int]
    ) -> list[tuple[str, str, str, str | None, int | None]]:
        records: list[tuple[str, str, str, str | None, int | None]] = []
        for source in self._category_sources:
            for path in source.paths:
                path_obj = Path(path)
                if not path_obj.exists():
                    continue
                for image_file in self._enumerate_image_files(path_obj):
                    status = self._resolve_status_from_path(str(image_file))
                    if source.label.upper() == "OK":
                        adms_process_id = None
                        for process_name, mapped_id in process_adms_mapping.items():
                            if process_name.lower() in str(image_file).lower():
                                adms_process_id = mapped_id
                                break
                        records.append((str(image_file), "OK", status, None, adms_process_id))
                    else:
                        label = source.label.upper()
                        records.append((str(image_file), label, status, label, None))
        return records

    async def _get_async(self, endpoint: str, model_cls):
        response = await self._client.get(f"{self._base_url}{endpoint}")
        response.raise_for_status()
        return model_cls.model_validate(response.json())

    async def _post_async(self, endpoint: str, payload: dict, model_cls):
        response = await self._client.post(f"{self._base_url}{endpoint}", json=payload)
        response.raise_for_status()
        return model_cls.model_validate(response.json())

    async def train_async(self, callback: TrainCallback | None = None) -> None:
        if not self._parameters:
            raise ValueError("Parameters not set")
        if not self._out_path:
            raise ValueError("Output path not set")
        if not self._category_sources:
            raise ValueError("No image categories loaded")

        request = PyClsTrainRequest.from_training_parameters(
            self._parameters,
            data_path=self._data_path or "",
            out_path=self._out_path,
            best_model_path=self._best_model_path,
            category_sources=self._category_sources,
        )
        self._last_request = request
        start_response = await self._post_async(
            "/train/cls/start",
            request.model_dump(),
            PyTrainStartResponse,
        )
        if start_response.result != "started":
            raise RuntimeError(f"Failed to start training: {start_response.result}")

        self._is_training = True
        self._cancel_event.clear()
        best_iteration = 0

        while not self._cancel_event.is_set():
            status_response = await self._get_async("/train/cls/status", PyTrainStatusResponse)
            self._last_status = status_response
            if status_response.current_accuracy >= status_response.best_accuracy and status_response.current_epoch > 0:
                best_iteration = status_response.current_epoch

            if callback:
                await callback(
                    status_response.running,
                    status_response.progress,
                    best_iteration,
                    status_response.current_accuracy,
                    status_response.best_accuracy,
                    status_response.best_accuracy,
                    1.0 - status_response.best_accuracy,
                )
            if not status_response.running:
                break
            await asyncio.sleep(self._poll_interval)

        self._is_training = False

    async def stop_training_async(self) -> PyTrainStopResponse:
        self._cancel_event.set()
        response = await self._post_async("/train/cls/stop", {}, PyTrainStopResponse)
        self._is_training = False
        return response

    async def stop_training(self) -> None:
        await self.stop_training_async()

    async def is_training(self) -> bool:
        try:
            status_response = await self._get_async("/train/cls/status", PyTrainStatusResponse)
            self._last_status = status_response
            return status_response.running
        except Exception:
            return self._is_training

    async def get_training_result(self) -> dict[str, float]:
        response = await self._client.get(f"{self._base_url}/train/cls/result")
        response.raise_for_status()
        data = response.json()
        result: dict[str, float] = {}
        for k, v in data.items():
            try:
                result[k] = float(v)
            except (TypeError, ValueError):
                continue
        return result

    async def get_best_model_path(self) -> str | None:
        if self._best_model_path:
            return self._best_model_path
        if self._last_status and self._last_status.best_model:
            return self._last_status.best_model
        response = await self._client.get(f"{self._base_url}/train/cls/result")
        if response.is_success:
            best_model = response.json().get("best_model")
            if best_model:
                return best_model
        return None

    async def get_log(self) -> str:
        response = await self._get_async("/train/cls/log", PyTrainLogResponse)
        return response.log

    async def get_confusion(self, true_class: str, predicted_class: str) -> int:
        response = await self._post_async(
            "/train/cls/confusion",
            {"true_class": true_class, "predicted_class": predicted_class},
            PyConfusionResponse,
        )
        return response.count

    async def get_confusion_matrix(self) -> PyConfusionMatrixResponse:
        return await self._post_async(
            "/train/cls/confusion",
            {"true_class": None, "predicted_class": None},
            PyConfusionMatrixResponse,
        )

    async def classify_async(
        self, image_path: str, weights_path: str | None = None
    ) -> PyClassificationResult:
        response: PyClassifyResponse = await self._post_async(
            "/infer/cls/single",
            {"image_path": image_path, "weights": weights_path},
            PyClassifyResponse,
        )
        if response.error:
            raise RuntimeError(response.error)
        return PyClassificationResult(
            best_label=response.bestLabel,
            best_score=response.bestScore,
            all_scores=response.allScores or {},
        )

    async def save_model_async(self, output_path: str, checkpoint_path: str | None = None) -> str:
        weights = checkpoint_path or await self.get_best_model_path()
        if not weights:
            raise RuntimeError("No model checkpoint available")
        request = PyOnnlPackRequest(weights=weights, out_path=output_path)
        response = await self._post_async(
            "/export/cls/onnl_pack", request.model_dump(), PyOnnlPackResponse
        )
        if response.error:
            raise RuntimeError(response.error)
        return response.path or output_path

    async def is_server_available(self) -> bool:
        try:
            response = await self._client.get(f"{self._base_url}/health")
            return response.is_success
        except Exception:
            return False

    async def wait_for_server(self, timeout_seconds: float) -> None:
        end_time = asyncio.get_running_loop().time() + timeout_seconds
        while asyncio.get_running_loop().time() < end_time:
            if await self.is_server_available():
                return
            await asyncio.sleep(0.5)
        raise TimeoutError("Training server is not available")

    def cleanup_temp_images(self) -> None:
        if self._temp_image_session_dir and Path(self._temp_image_session_dir).exists():
            shutil.rmtree(self._temp_image_session_dir, ignore_errors=True)

    async def aclose(self) -> None:
        self.cleanup_temp_images()
        await self._client.aclose()
