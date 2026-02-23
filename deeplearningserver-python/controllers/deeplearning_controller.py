from fastapi import APIRouter, HTTPException
import httpx

from config import settings
from dtos import ImageSize, TrainingDto
from services.mssql_db_service import MssqlDbService
from services.tool_status_manager import ToolStatusManager
from services.training_bridge import TrainingAiHttpBridge

router = APIRouter()
db_service = MssqlDbService()

@router.post("/run", response_model=dict)
async def create_tool_and_run(parameter_data: TrainingDto):
    return {
        "message": "Training endpoint - implementation pending",
        "status": "not_implemented",
        "received_image_size": parameter_data.image_size.value,
        "received_adms_process_count": len(parameter_data.adms_process_ids),
    }


@router.delete("/stop", response_model=dict)
async def stop_training():
    try:
        if settings.use_python_server:
            bridge = TrainingAiHttpBridge.current_instance
            if bridge is not None:
                await bridge.stop_training()
                TrainingAiHttpBridge.set_current_instance(None)
        ToolStatusManager.set_process_running(False)
        return {
            "message": "Training stopped and all resources disposed successfully",
            "status": "Success",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/result/{image_size}", response_model=dict)
async def get_training_result(image_size: ImageSize):
    try:
        # Return latest training labels/metrics by image size.
        from services.db_service import SessionLocal
        from models import TrainingRecord, Label

        db = SessionLocal()
        try:
            latest = (
                db.query(TrainingRecord)
                .filter(TrainingRecord.image_size == (1 if image_size == ImageSize.LARGE else 0))
                .order_by(TrainingRecord.created_time.desc())
                .first()
            )
            if not latest:
                return {"message": "No training record found", "image_size": image_size.value, "labels": []}
            labels = db.query(Label).filter(Label.training_record_id == latest.id).all()
            return {
                "training_record_id": latest.id,
                "image_size": image_size.value,
                "status": latest.status,
                "accuracy": latest.accuracy,
                "loss": latest.loss,
                "progress": latest.progress,
                "labels": [{"name": item.name, "accuracy": item.accuracy} for item in labels],
            }
        finally:
            db.close()
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/confusion/{image_size}/{true_label}/{predicted_label}", response_model=dict)
async def get_confusion_count(image_size: ImageSize, true_label: str, predicted_label: str):
    try:
        from services.db_service import SessionLocal
        from models import TrainingRecord

        db = SessionLocal()
        try:
            latest = (
                db.query(TrainingRecord)
                .filter(TrainingRecord.image_size == (1 if image_size == ImageSize.LARGE else 0))
                .order_by(TrainingRecord.created_time.desc())
                .first()
            )
        finally:
            db.close()
        if not latest:
            return {"count": 0}
        matrix = await db_service.get_training_confusion_matrix(latest.id)
        count = 0
        for row in matrix:
            if (
                row["trueLabel"].upper() == true_label.upper()
                and row["predictedLabel"].upper() == predicted_label.upper()
            ):
                count = row["count"]
                break
        return {"count": count}
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/getConfusionMatrix/{training_record_id}", response_model=dict)
async def get_confusion_matrix(training_record_id: int):
    try:
        matrix = await db_service.get_training_confusion_matrix(training_record_id)
        return {
            "training_record_id": training_record_id,
            "confusion_matrix": matrix,
            "message": "Dynamically calculated from TrainingImageResult table",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/getConfusionMatrixImages/{training_record_id}/{true_label}/{predicted_label}", response_model=dict)
async def get_confusion_matrix_images(training_record_id: int, true_label: str, predicted_label: str):
    try:
        images = await db_service.get_training_images_by_labels(training_record_id, true_label, predicted_label)
        return {
            "training_record_id": training_record_id,
            "true_label": true_label,
            "predicted_label": predicted_label,
            "image_count": len(images),
            "images": images,
            "message": "Retrieved from simplified TrainingImageResult table",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/getConfusionMatrixImageFiles/{training_record_id}/{true_label}/{predicted_label}", response_model=dict)
async def get_confusion_matrix_image_files(training_record_id: int, true_label: str, predicted_label: str):
    try:
        images = await db_service.get_training_images_by_labels(training_record_id, true_label, predicted_label)
        image_files = [row["imageFile"] for row in images if row.get("imageFile")]
        return {
            "training_record_id": training_record_id,
            "true_label": true_label,
            "predicted_label": predicted_label,
            "image_count": len(image_files),
            "image_files": image_files,
            "message": "Retrieved from simplified TrainingImageResult table",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/load/{image_size}", response_model=dict)
async def load_model(image_size: ImageSize, model_file_path: str = "", settings_file_path: str = ""):
    try:
        if not settings.use_python_server:
            return {"status": "Ok", "message": "Python server disabled, skipped."}
        async with httpx.AsyncClient(timeout=30.0) as client:
            response = await client.post(
                f"{settings.python_training_server_url}/infer/cls/load",
                json={"image_size": image_size.value, "model_file_path": model_file_path, "settings_file_path": settings_file_path},
            )
            if response.is_success:
                return {"status": "Ok", "response": response.json() if response.text else {}}
            raise HTTPException(status_code=response.status_code, detail=response.text)
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc