from fastapi import APIRouter, Depends, HTTPException
from typing import List, Dict, Any
from pydantic import BaseModel
from enum import Enum
from services.db_service import get_db
from config import settings

router = APIRouter()

class ImageSize(str, Enum):
    MIDDLE = "Middle"
    LARGE = "Large"

class ProcessImageCountRequest(BaseModel):
    adms_process_ids: List[int]
    image_size: ImageSize

class ProcessImageCountResult(BaseModel):
    adms_process_id: int
    process_id: int
    process_name: str
    image_details: Dict[str, int]
    total_images: int

@router.get("/categories/{image_size}", response_model=Dict[str, Any])
async def get_categories_by_image_size(image_size: ImageSize, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "image_size": image_size.value,
            "image_path": settings.middle_image_path if image_size == ImageSize.MIDDLE else settings.large_image_path,
            "categories": [],
            "category_counts": {},
            "count": 0,
            "total_images": 0,
            "source": "Database"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/sync-ng/{image_size}", response_model=Dict[str, Any])
async def sync_ng_images(image_size: ImageSize, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would sync images
        return {
            "sync": {"status": "completed"},
            "categories": [],
            "category_counts": {},
            "total_images": 0,
            "source": "Database"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/sync-ok/{adms_process_id}/{image_size}", response_model=Dict[str, Any])
async def sync_ok_images(adms_process_id: int, image_size: ImageSize, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would sync images
        return {
            "sync": {"status": "completed"},
            "image_details": {},
            "total_images": 0,
            "source": "Database"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/process-images/{adms_process_id}/{image_size}", response_model=Dict[str, Any])
async def get_process_image_count(adms_process_id: int, image_size: ImageSize, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "adms_process_id": adms_process_id,
            "process_id": 1,
            "process_name": "Test Process",
            "image_size": image_size.value,
            "image_path": settings.middle_image_path if image_size == ImageSize.MIDDLE else settings.large_image_path,
            "image_details": {"BASE": 0, "NEW": 0},
            "total_images": 0,
            "source": "Database"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/process-images-bulk", response_model=Dict[str, Any])
async def get_process_image_count_bulk(request: ProcessImageCountRequest, db = Depends(get_db)):
    try:
        # This is a simplified version - actual implementation would query database
        return {
            "image_size": request.image_size.value,
            "image_path": settings.middle_image_path if request.image_size == ImageSize.MIDDLE else settings.large_image_path,
            "processed_count": 0,
            "total_processes": len(request.adms_process_ids),
            "total_all_images": 0,
            "results": [],
            "source": "Database"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))