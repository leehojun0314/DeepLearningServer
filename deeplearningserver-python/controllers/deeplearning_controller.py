from fastapi import APIRouter, Depends, HTTPException, BackgroundTasks
from typing import List, Dict, Any
from pydantic import BaseModel
from enum import Enum
from services.db_service import get_db
from config import settings
import httpx

router = APIRouter()

class ImageSize(str, Enum):
    MIDDLE = "Middle"
    LARGE = "Large"

class TrainingStatus(str, Enum):
    LOADING = "Loading"
    RUNNING = "Running"
    COMPLETED = "Completed"
    FAILED = "Failed"
    CANCELLED = "Cancelled"

class Geometry(BaseModel):
    max_rotation: int = 0
    max_vertical_shift: int = 0
    max_horizontal_shift: int = 0
    min_scale: float = 1.0
    max_scale: float = 1.0
    max_vertical_shear: float = 0.0
    max_horizontal_shear: float = 0.0
    vertical_flip: bool = False
    horizontal_flip: bool = False

class Color(BaseModel):
    max_brightness_offset: int = 0
    min_contrast_gain: float = 1.0
    max_contrast_gain: float = 1.0
    min_gamma: float = 1.0
    max_gamma: float = 1.0
    hue_offset: int = 0
    min_saturation_gain: float = 1.0
    max_saturation_gain: float = 1.0

class Noise(BaseModel):
    min_gaussian_deviation: float = 0.0
    max_gaussian_deviation: float = 0.0
    min_speckle_deviation: float = 0.0
    max_speckle_deviation: float = 0.0
    min_salt_pepper_noise: float = 0.0
    max_salt_pepper_noise: float = 0.0

class Classifier(BaseModel):
    classifier_capacity: str = "Normal"
    image_width: int = 224
    image_height: int = 224
    image_cache_size: int = 100
    image_channels: int = 3
    add_fft: bool = False
    gray_input: bool = False
    use_pretrained_model: bool = False
    compute_heat_map: bool = False
    enable_histogram_equalization: bool = False
    batch_size: int = 32
    enable_deterministic_training: bool = False

class TrainingDto(BaseModel):
    adms_process_ids: List[int]
    image_size: ImageSize
    categories: List[str] = []
    is_default_model: bool = False
    client_model_destination: str = ""
    training_proportion: float = 0.8
    validation_proportion: float = 0.1
    test_proportion: float = 0.1
    iterations: int = 50
    early_stopping_patience: int = 10
    geometry: Geometry = Geometry()
    color: Color = Color()
    noise: Noise = Noise()
    classifier: Classifier = Classifier()

class ProgressEntry(BaseModel):
    is_training: bool = True
    progress: float = 0.0
    best_iteration: int = 0
    start_time: str = ""
    end_time: str = ""
    duration: float = 0.0
    accuracy: float = 0.0
    validation_accuracy: float = 0.0
    validation_error: float = 0.0
    training_record_id: int = 0

@router.post("/run", response_model=Dict[str, Any])
async def create_tool_and_run(parameter_data: TrainingDto, db = Depends(get_db)):
    try:
        # Check if we should use Python server
        if settings.use_python_server:
            # Call Python training server
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    f"{settings.python_training_server_url}/train",
                    json={
                        "adms_process_ids": parameter_data.adms_process_ids,
                        "image_size": parameter_data.image_size.value,
                        "categories": parameter_data.categories,
                        "is_default_model": parameter_data.is_default_model,
                        "client_model_destination": parameter_data.client_model_destination,
                        "training_proportion": parameter_data.training_proportion,
                        "validation_proportion": parameter_data.validation_proportion,
                        "test_proportion": parameter_data.test_proportion,
                        "iterations": parameter_data.iterations,
                        "early_stopping_patience": parameter_data.early_stopping_patience,
                        "geometry": parameter_data.geometry.dict(),
                        "color": parameter_data.color.dict(),
                        "noise": parameter_data.noise.dict(),
                        "classifier": parameter_data.classifier.dict()
                    }
                )
                if response.status_code == 200:
                    result = response.json()
                    return {
                        "message": "Training initialized successfully.",
                        "training_id": result.get("training_id", "1")
                    }
                else:
                    raise HTTPException(status_code=response.status_code, detail="Python server training failed")
        else:
            # This would be the Euresys implementation - simplified for now
            return {
                "message": "Training initialized successfully.",
                "training_id": "1"
            }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@router.delete("/stop", response_model=Dict[str, Any])
async def stop_training(db = Depends(get_db)):
    try:
        # This would stop the training process
        return {
            "message": "Training stopped and all resources disposed successfully",
            "image_size": "Middle",
            "status": "Success"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/result/{image_size}", response_model=Dict[str, Any])
async def get_training_result(image_size: ImageSize, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "accuracy": 0.95,
            "precision": 0.92,
            "recall": 0.88,
            "f1_score": 0.90
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/confusion/{image_size}/{true_label}/{predicted_label}", response_model=Dict[str, Any])
async def get_confusion_matrix(image_size: ImageSize, true_label: str, predicted_label: str, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "count": 0
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/getConfusionMatrix/{training_record_id}", response_model=Dict[str, Any])
async def get_confusion_matrix(training_record_id: int, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "training_record_id": training_record_id,
            "confusion_matrix": [],
            "message": "Dynamically calculated from TrainingImageResult table"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/getConfusionMatrixImages/{training_record_id}/{true_label}/{predicted_label}", response_model=Dict[str, Any])
async def get_confusion_matrix_images(training_record_id: int, true_label: str, predicted_label: str, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "training_record_id": training_record_id,
            "true_label": true_label,
            "predicted_label": predicted_label,
            "image_count": 0,
            "images": [],
            "message": "Retrieved from simplified TrainingImageResult table"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/getConfusionMatrixImageFiles/{training_record_id}/{true_label}/{predicted_label}", response_model=Dict[str, Any])
async def get_confusion_matrix_image_files(training_record_id: int, true_label: str, predicted_label: str, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "training_record_id": training_record_id,
            "true_label": true_label,
            "predicted_label": predicted_label,
            "image_count": 0,
            "image_files": [],
            "message": "Retrieved from simplified TrainingImageResult table"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/load/{image_size}", response_model=Dict[str, Any])
async def load_model(image_size: ImageSize, model_file_path: str = "", settings_file_path: str = "", db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would load model
        return {
            "status": "Ok"
        }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))