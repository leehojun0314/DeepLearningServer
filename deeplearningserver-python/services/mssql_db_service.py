from __future__ import annotations

from collections import defaultdict
from contextlib import contextmanager
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any

from sqlalchemy import func, or_
from sqlalchemy.orm import Session, joinedload

from models import (
    Adm,
    AdmsProcess,
    AdmsProcessType,
    ImageFile,
    Label,
    LogRecord,
    ModelRecord,
    Process,
    ProgressEntry,
    TrainingAdmsProcess,
    TrainingImageResult,
    TrainingRecord,
)
from services.db_service import SessionLocal


@dataclass
class NgSyncResult:
    image_size: str
    image_root: str
    total_files_scanned: int = 0
    inserted: int = 0
    skipped: int = 0
    inserted_by_category: dict[str, int] | None = None
    errors: list[str] | None = None

    def __post_init__(self):
        self.inserted_by_category = self.inserted_by_category or {}
        self.errors = self.errors or []


@dataclass
class OkSyncResult:
    image_size: str
    image_root: str
    adms_process_id: int
    process_name: str = ""
    total_files_scanned: int = 0
    inserted: int = 0
    skipped: int = 0
    inserted_base: int = 0
    inserted_new: int = 0
    errors: list[str] | None = None

    def __post_init__(self):
        self.errors = self.errors or []


class MssqlDbService:
    @contextmanager
    def _db(self):
        db = SessionLocal()
        try:
            yield db
            db.commit()
        except Exception:
            db.rollback()
            raise
        finally:
            db.close()

    @staticmethod
    def convert_to_relative_path(absolute_path: str) -> str:
        if not absolute_path:
            return ""
        path = absolute_path
        if len(path) >= 3 and path[1] == ":" and path[2] == "\\":
            path = path[3:]
        return path.replace("\\", "/")

    @staticmethod
    def _image_size_to_str(image_size: str) -> str:
        return "Large" if str(image_size).lower() == "large" else "Middle"

    async def insert_log(self, message: str, level: str = "Information") -> None:
        with self._db() as db:
            db.add(LogRecord(message=message, level=level, created_at=datetime.now()))

    async def get_adms_process(self, adms_id: int, process_id: int) -> AdmsProcess:
        with self._db() as db:
            obj = (
                db.query(AdmsProcess)
                .filter(AdmsProcess.adms_id == adms_id, AdmsProcess.process_id == process_id)
                .first()
            )
            if not obj:
                raise ValueError("AdmsProcess not found")
            return obj

    async def get_process_name_by_id(self, process_id: int) -> str:
        with self._db() as db:
            obj = db.query(Process).filter(Process.id == process_id).first()
            if not obj:
                raise ValueError("Process not found")
            return obj.name

    async def get_adms_by_id(self, adms_id: int) -> Adm:
        with self._db() as db:
            obj = db.query(Adm).filter(Adm.id == adms_id).first()
            if not obj:
                raise ValueError("Adms not found")
            return obj

    async def get_adms_process_infos(self, adms_process_ids: list[int]) -> list[dict[str, Any]]:
        with self._db() as db:
            rows = db.query(AdmsProcess).filter(AdmsProcess.id.in_(adms_process_ids)).all()
            result: list[dict[str, Any]] = []
            for row in rows:
                process = db.query(Process).filter(Process.id == row.process_id).first()
                result.append(
                    {
                        "admsId": row.adms_id,
                        "processId": row.process_id,
                        "processName": process.name if process else "",
                        "admsProcessId": row.id,
                    }
                )
            return result

    async def insert_training(self, training_record: TrainingRecord) -> TrainingRecord:
        with self._db() as db:
            db.add(training_record)
            db.flush()
            db.refresh(training_record)
            return training_record

    async def update_training(self, training_record: TrainingRecord) -> None:
        with self._db() as db:
            db.merge(training_record)

    async def partial_update_training(self, training_id: int, updates: dict[str, Any]) -> None:
        with self._db() as db:
            obj = db.query(TrainingRecord).filter(TrainingRecord.id == training_id).first()
            if not obj:
                return
            for key, value in updates.items():
                if hasattr(obj, key):
                    setattr(obj, key, value)

    async def add_range_training_adms_process(
        self, training_adms_processes: list[TrainingAdmsProcess]
    ) -> None:
        with self._db() as db:
            db.add_all(training_adms_processes)

    async def get_or_create_adms_process_type(
        self, adms_process_id: int, type_name: str
    ) -> AdmsProcessType:
        with self._db() as db:
            row = (
                db.query(AdmsProcessType)
                .filter(
                    AdmsProcessType.adms_process_id == adms_process_id,
                    AdmsProcessType.type == type_name,
                )
                .first()
            )
            if row:
                return row
            row = AdmsProcessType(
                adms_process_id=adms_process_id,
                type=type_name,
                is_categorized=False,
                is_trainned=True,
                last_sync_date=datetime.now(),
            )
            db.add(row)
            db.flush()
            db.refresh(row)
            return row

    async def push_progress_entry(self, progress_entry: ProgressEntry) -> None:
        with self._db() as db:
            db.add(progress_entry)

    async def update_progress_entry(self, progress_entry: ProgressEntry) -> None:
        with self._db() as db:
            db.merge(progress_entry)

    async def update_labels_by_id(self, training_record_id: int, labels: list[Label]) -> None:
        with self._db() as db:
            db.query(Label).filter(Label.training_record_id == training_record_id).delete()
            for label in labels:
                label.training_record_id = training_record_id
                db.add(label)

    async def check_is_training(self) -> bool:
        with self._db() as db:
            return (
                db.query(TrainingRecord)
                .filter(func.lower(TrainingRecord.status) == "running")
                .first()
                is not None
            )

    async def save_training_images(
        self,
        training_image_records: list[tuple[str, str, str, str | None, int | None]],
        image_size: str = "Middle",
    ) -> None:
        size = self._image_size_to_str(image_size)
        with self._db() as db:
            for image_path, _, status, category, adms_process_id in training_image_records:
                file_name = Path(image_path).name
                directory = self.convert_to_relative_path(str(Path(image_path).parent))
                query = db.query(ImageFile).filter(
                    ImageFile.name == file_name, ImageFile.directory == directory
                )
                if adms_process_id:
                    query = query.filter(ImageFile.adms_process_id == adms_process_id)
                elif category:
                    query = query.filter(ImageFile.category == category)
                existing = query.first()
                if existing:
                    existing.status = "Training"
                else:
                    db.add(
                        ImageFile(
                            name=file_name,
                            directory=directory,
                            size=size,
                            status=status,
                            adms_process_id=adms_process_id,
                            category=category,
                            captured_time=datetime.now(),
                        )
                    )

    async def save_training_image_result(
        self,
        training_record_id: int,
        image_file_id: int,
        true_label: str,
        predicted_label: str,
        confidence: float | None = None,
        status: str = "Predicted",
        category: str | None = None,
        adms_process_id: int | None = None,
    ) -> None:
        with self._db() as db:
            db.add(
                TrainingImageResult(
                    training_record_id=training_record_id,
                    image_file_id=image_file_id,
                    true_label=true_label.upper(),
                    predicted_label=predicted_label.upper(),
                    confidence=confidence,
                    status=status,
                    category=category,
                    adms_process_id=adms_process_id,
                    created_at=datetime.now(),
                )
            )

    async def get_training_confusion_matrix(self, training_record_id: int) -> list[dict[str, Any]]:
        with self._db() as db:
            rows = (
                db.query(
                    TrainingImageResult.true_label,
                    TrainingImageResult.predicted_label,
                    func.count(TrainingImageResult.id).label("count"),
                    func.avg(TrainingImageResult.confidence).label("avg_confidence"),
                )
                .filter(TrainingImageResult.training_record_id == training_record_id)
                .group_by(TrainingImageResult.true_label, TrainingImageResult.predicted_label)
                .order_by(TrainingImageResult.true_label, TrainingImageResult.predicted_label)
                .all()
            )
            return [
                {
                    "trueLabel": row.true_label,
                    "predictedLabel": row.predicted_label,
                    "count": row.count,
                    "avgConfidence": float(row.avg_confidence or 0.0),
                }
                for row in rows
            ]

    async def get_training_images_by_labels(
        self, training_record_id: int, true_label: str, predicted_label: str
    ) -> list[dict[str, Any]]:
        with self._db() as db:
            rows = (
                db.query(TrainingImageResult)
                .options(joinedload(TrainingImageResult.image_file))
                .filter(
                    TrainingImageResult.training_record_id == training_record_id,
                    TrainingImageResult.true_label == true_label.upper(),
                    TrainingImageResult.predicted_label == predicted_label.upper(),
                )
                .order_by(TrainingImageResult.confidence.desc())
                .all()
            )
            return [
                {
                    "id": row.id,
                    "trueLabel": row.true_label,
                    "predictedLabel": row.predicted_label,
                    "confidence": row.confidence,
                    "createdAt": row.created_at.isoformat() if row.created_at else None,
                    "imageFile": {
                        "id": row.image_file.id,
                        "name": row.image_file.name,
                        "directory": row.image_file.directory,
                        "size": row.image_file.size,
                        "status": row.image_file.status,
                        "capturedTime": row.image_file.captured_time.isoformat()
                        if row.image_file.captured_time
                        else None,
                    }
                    if row.image_file
                    else None,
                }
                for row in rows
            ]

    async def find_image_file(
        self, file_name: str, directory: str, adms_process_id: int | None = None, category: str | None = None
    ) -> ImageFile | None:
        with self._db() as db:
            query = db.query(ImageFile).filter(ImageFile.name == file_name, ImageFile.directory == directory)
            if adms_process_id is not None:
                query = query.filter(ImageFile.adms_process_id == adms_process_id)
            if category:
                query = query.filter(ImageFile.category == category)
            return query.first()

    async def insert_model_record(self, model_record: ModelRecord) -> None:
        with self._db() as db:
            db.add(model_record)

    async def get_ng_categories_image_count(self, image_size: str) -> dict[str, int]:
        size = self._image_size_to_str(image_size)
        with self._db() as db:
            rows = (
                db.query(ImageFile)
                .filter(
                    ImageFile.size == size,
                    or_(ImageFile.directory.contains("/NG/BASE/"), ImageFile.directory.contains("/NG/NEW/")),
                )
                .all()
            )
            result: dict[str, int] = defaultdict(int)
            for row in rows:
                parts = row.directory.split("/")
                if "NG" in parts:
                    idx = parts.index("NG")
                    if idx + 2 < len(parts):
                        result[parts[idx + 2].upper()] += 1
            return dict(result)

    async def get_ok_image_count_by_process(self, adms_process_id: int, image_size: str) -> dict[str, int]:
        size = self._image_size_to_str(image_size)
        with self._db() as db:
            rows = (
                db.query(ImageFile)
                .filter(
                    ImageFile.adms_process_id == adms_process_id,
                    ImageFile.size == size,
                    ImageFile.directory.contains("/OK/"),
                )
                .all()
            )
            result = {"BASE": 0, "NEW": 0}
            for row in rows:
                if "/BASE" in row.directory:
                    result["BASE"] += 1
                elif "/NEW" in row.directory:
                    result["NEW"] += 1
            return result

    async def get_ok_image_count_bulk(
        self, adms_process_ids: list[int], image_size: str
    ) -> dict[int, dict[str, int]]:
        output = {pid: {"BASE": 0, "NEW": 0} for pid in adms_process_ids}
        for pid in adms_process_ids:
            output[pid] = await self.get_ok_image_count_by_process(pid, image_size)
        return output

    async def sync_ng_images(self, image_size: str, image_root_path: str) -> NgSyncResult:
        size = self._image_size_to_str(image_size)
        result = NgSyncResult(image_size=size, image_root=image_root_path)
        root = Path(image_root_path)
        if not root.exists():
            result.errors.append(f"Image root path does not exist: {image_root_path}")
            return result
        files: list[tuple[Path, str]] = []
        for status in ("BASE", "NEW"):
            path = root / "NG" / status
            if path.exists():
                for ext in ("*.jpg", "*.png"):
                    files.extend((f, status.title()) for f in path.rglob(ext))
        result.total_files_scanned = len(files)
        with self._db() as db:
            for file_path, status in files:
                rel_dir = self.convert_to_relative_path(str(file_path.parent))
                parts = rel_dir.split("/")
                category = ""
                if "NG" in parts:
                    idx = parts.index("NG")
                    if idx + 2 < len(parts):
                        category = parts[idx + 2].upper()
                if not category:
                    result.skipped += 1
                    continue
                exists = (
                    db.query(ImageFile)
                    .filter(
                        ImageFile.name == file_path.name,
                        ImageFile.directory == rel_dir,
                        ImageFile.category == category,
                    )
                    .first()
                )
                if exists:
                    result.skipped += 1
                    continue
                db.add(
                    ImageFile(
                        name=file_path.name,
                        directory=rel_dir,
                        size=size,
                        status=status,
                        adms_process_id=None,
                        category=category,
                        captured_time=datetime.fromtimestamp(file_path.stat().st_ctime),
                    )
                )
                result.inserted += 1
                result.inserted_by_category[category] = result.inserted_by_category.get(category, 0) + 1
        return result

    async def sync_ok_images_by_process(
        self, adms_process_id: int, image_size: str, image_root_path: str
    ) -> OkSyncResult:
        size = self._image_size_to_str(image_size)
        result = OkSyncResult(image_size=size, image_root=image_root_path, adms_process_id=adms_process_id)
        root = Path(image_root_path)
        if not root.exists():
            result.errors.append(f"Image root path does not exist: {image_root_path}")
            return result
        with self._db() as db:
            adms_process = (
                db.query(AdmsProcess)
                .options(joinedload(AdmsProcess.process))
                .filter(AdmsProcess.id == adms_process_id)
                .first()
            )
            if not adms_process or not adms_process.process:
                result.errors.append(f"AdmsProcess not found: {adms_process_id}")
                return result
            result.process_name = adms_process.process.name
            files: list[tuple[Path, str]] = []
            for status in ("BASE", "NEW"):
                base_dir = root / "OK" / result.process_name / status
                if base_dir.exists():
                    for ext in ("*.jpg", "*.png"):
                        files.extend((f, status.title()) for f in base_dir.rglob(ext))
            result.total_files_scanned = len(files)
            for file_path, status in files:
                rel_dir = self.convert_to_relative_path(str(file_path.parent))
                exists = (
                    db.query(ImageFile)
                    .filter(
                        ImageFile.name == file_path.name,
                        ImageFile.directory == rel_dir,
                        ImageFile.adms_process_id == adms_process_id,
                    )
                    .first()
                )
                if exists:
                    result.skipped += 1
                    continue
                db.add(
                    ImageFile(
                        name=file_path.name,
                        directory=rel_dir,
                        size=size,
                        status=status,
                        adms_process_id=adms_process_id,
                        category=None,
                        captured_time=datetime.fromtimestamp(file_path.stat().st_ctime),
                    )
                )
                result.inserted += 1
                if status == "Base":
                    result.inserted_base += 1
                elif status == "New":
                    result.inserted_new += 1
        return result
