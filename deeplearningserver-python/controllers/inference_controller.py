from fastapi import APIRouter, HTTPException
from config import settings
import httpx

from dtos import InferenceDto, MultiInferenceDto

router = APIRouter()


@router.post("/single", response_model=dict)
async def post_single(inference_dto: InferenceDto):
    try:
        if settings.use_python_server:
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    f"{settings.python_training_server_url}/infer/cls/single",
                    json={
                        "image_path": inference_dto.image_path,
                        "weights": inference_dto.model_path,
                    }
                )
                if response.status_code == 200:
                    result = response.json()
                    return {
                        "best_label": result.get("bestLabel"),
                        "best_probability": result.get("bestScore"),
                        "elapsed_milliseconds": result.get("elapsed_ms", 0),
                        "label_probabilities": result.get("allScores", {}),
                    }
                raise HTTPException(status_code=response.status_code, detail="Python server inference failed")

        return {
            "best_label": "OK",
            "best_probability": 0.95,
            "elapsed_milliseconds": 120,
            "label_probabilities": {"OK": 0.95, "NG": 0.05},
        }
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.post("/multi", response_model=list[dict])
async def post_multi(inference_dto: MultiInferenceDto):
    try:
        if settings.use_python_server:
            results = []
            async with httpx.AsyncClient() as client:
                for image_path in inference_dto.image_paths:
                    response = await client.post(
                        f"{settings.python_training_server_url}/infer/cls/single",
                        json={
                            "image_path": image_path,
                            "weights": inference_dto.model_path,
                        }
                    )
                    if response.status_code == 200:
                        result = response.json()
                        results.append({
                            "best_label": result.get("bestLabel"),
                            "best_probability": result.get("bestScore"),
                            "label_probabilities": [
                                {"label": k, "probability": v} 
                                for k, v in result.get("allScores", {}).items()
                            ]
                        })
                    else:
                        results.append({
                            "best_label": "ERROR",
                            "best_probability": 0.0,
                            "label_probabilities": []
                        })
            return results

        results = []
        for i, _ in enumerate(inference_dto.image_paths):
            results.append(
                {
                    "best_label": "OK" if i % 2 == 0 else "NG",
                    "best_probability": 0.95 if i % 2 == 0 else 0.85,
                    "label_probabilities": [
                        {"label": "OK", "probability": 0.95 if i % 2 == 0 else 0.85},
                        {"label": "NG", "probability": 0.05 if i % 2 == 0 else 0.15},
                    ],
                }
            )
        return results
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc