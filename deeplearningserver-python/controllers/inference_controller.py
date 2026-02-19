from fastapi import APIRouter, Depends, HTTPException
from typing import List, Dict, Any
from pydantic import BaseModel
from services.db_service import get_db
from config import settings
import httpx

router = APIRouter()

class InferenceDto(BaseModel):
    model_path: str
    image_path: str

class MultiInferenceDto(BaseModel):
    model_path: str
    image_paths: List[str]

class ClassificationResultDto(BaseModel):
    best_label: str
    best_probability: float
    label_probabilities: List[Dict[str, Any]]

@router.post("/single", response_model=Dict[str, Any])
async def post_single(inference_dto: InferenceDto, db = Depends(get_db)):
    try:
        # Check if we should use Python server
        if settings.use_python_server:
            # Call Python training server
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    f"{settings.python_training_server_url}/classify",
                    json={
                        "image_path": inference_dto.image_path,
                        "model_path": inference_dto.model_path
                    }
                )
                if response.status_code == 200:
                    result = response.json()
                    return {
                        "best_label": result.get("best_label"),
                        "best_probability": result.get("best_score"),
                        "elapsed_milliseconds": result.get("elapsed_ms", 0),
                        "label_probabilities": result.get("all_scores", {})
                    }
                else:
                    raise HTTPException(status_code=response.status_code, detail="Python server inference failed")
        else:
            # This would be the Euresys implementation - simplified for now
            return {
                "best_label": "OK",
                "best_probability": 0.95,
                "elapsed_milliseconds": 120,
                "label_probabilities": {"OK": 0.95, "NG": 0.05}
            }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@router.post("/multi", response_model=List[Dict[str, Any]])
async def post_multi(inference_dto: MultiInferenceDto, db = Depends(get_db)):
    try:
        # Check if we should use Python server
        if settings.use_python_server:
            # Call Python training server for each image
            results = []
            async with httpx.AsyncClient() as client:
                for image_path in inference_dto.image_paths:
                    response = await client.post(
                        f"{settings.python_training_server_url}/classify",
                        json={
                            "image_path": image_path,
                            "model_path": inference_dto.model_path
                        }
                    )
                    if response.status_code == 200:
                        result = response.json()
                        results.append({
                            "best_label": result.get("best_label"),
                            "best_probability": result.get("best_score"),
                            "label_probabilities": [
                                {"label": k, "probability": v} 
                                for k, v in result.get("all_scores", {}).items()
                            ]
                        })
                    else:
                        results.append({
                            "best_label": "ERROR",
                            "best_probability": 0.0,
                            "label_probabilities": []
                        })
            return results
        else:
            # This would be the Euresys implementation - simplified for now
            results = []
            for i, image_path in enumerate(inference_dto.image_paths):
                results.append({
                    "best_label": "OK" if i % 2 == 0 else "NG",
                    "best_probability": 0.95 if i % 2 == 0 else 0.85,
                    "label_probabilities": [
                        {"label": "OK", "probability": 0.95 if i % 2 == 0 else 0.85},
                        {"label": "NG", "probability": 0.05 if i % 2 == 0 else 0.15}
                    ]
                })
            return results
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))