from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from config import settings
from dtos import ImageSize
from services.mssql_db_service import MssqlDbService

router = APIRouter()
db_service = MssqlDbService()


class ProcessImageCountRequest(BaseModel):
    adms_process_ids: list[int]
    image_size: ImageSize


@router.get("/categories/{image_size}", response_model=dict)
async def get_categories_by_image_size(image_size: ImageSize):
    try:
        image_path = settings.middle_image_path if image_size == ImageSize.MIDDLE else settings.large_image_path
        category_counts = await db_service.get_ng_categories_image_count(image_size.value)
        return {
            "image_size": image_size.value,
            "image_path": image_path,
            "categories": sorted(category_counts.keys()),
            "category_counts": category_counts,
            "count": len(category_counts),
            "total_images": sum(category_counts.values()),
            "source": "Database",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.post("/sync-ng/{image_size}", response_model=dict)
async def sync_ng_images(image_size: ImageSize):
    try:
        image_path = settings.middle_image_path if image_size == ImageSize.MIDDLE else settings.large_image_path
        sync_result = await db_service.sync_ng_images(image_size.value, image_path)
        category_counts = await db_service.get_ng_categories_image_count(image_size.value)
        return {
            "sync": sync_result.__dict__,
            "categories": sorted(category_counts.keys()),
            "category_counts": category_counts,
            "total_images": sum(category_counts.values()),
            "source": "Database",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.post("/sync-ok/{adms_process_id}/{image_size}", response_model=dict)
async def sync_ok_images(adms_process_id: int, image_size: ImageSize):
    try:
        image_path = settings.middle_image_path if image_size == ImageSize.MIDDLE else settings.large_image_path
        sync_result = await db_service.sync_ok_images_by_process(adms_process_id, image_size.value, image_path)
        image_details = await db_service.get_ok_image_count_by_process(adms_process_id, image_size.value)
        return {
            "sync": sync_result.__dict__,
            "image_details": image_details,
            "total_images": image_details.get("BASE", 0) + image_details.get("NEW", 0),
            "source": "Database",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.get("/process-images/{adms_process_id}/{image_size}", response_model=dict)
async def get_process_image_count(adms_process_id: int, image_size: ImageSize):
    try:
        image_path = settings.middle_image_path if image_size == ImageSize.MIDDLE else settings.large_image_path
        image_details = await db_service.get_ok_image_count_by_process(adms_process_id, image_size.value)
        process_info = await db_service.get_adms_process_infos([adms_process_id])
        process = process_info[0] if process_info else {}
        return {
            "adms_process_id": adms_process_id,
            "process_id": process.get("processId", 0),
            "process_name": process.get("processName", ""),
            "image_size": image_size.value,
            "image_path": image_path,
            "image_details": image_details,
            "total_images": image_details.get("BASE", 0) + image_details.get("NEW", 0),
            "source": "Database",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@router.post("/process-images-bulk", response_model=dict)
async def get_process_image_count_bulk(request: ProcessImageCountRequest):
    try:
        image_path = (
            settings.middle_image_path
            if request.image_size == ImageSize.MIDDLE
            else settings.large_image_path
        )
        bulk = await db_service.get_ok_image_count_bulk(request.adms_process_ids, request.image_size.value)
        process_infos = await db_service.get_adms_process_infos(request.adms_process_ids)
        info_map = {item["admsProcessId"]: item for item in process_infos}

        results = []
        total_all_images = 0
        for adms_process_id in request.adms_process_ids:
            details = bulk.get(adms_process_id, {"BASE": 0, "NEW": 0})
            total = details.get("BASE", 0) + details.get("NEW", 0)
            total_all_images += total
            info = info_map.get(adms_process_id, {})
            results.append(
                {
                    "adms_process_id": adms_process_id,
                    "process_id": info.get("processId", 0),
                    "process_name": info.get("processName", ""),
                    "image_details": details,
                    "total_images": total,
                }
            )

        return {
            "image_size": request.image_size.value,
            "image_path": image_path,
            "processed_count": len(results),
            "total_processes": len(request.adms_process_ids),
            "total_all_images": total_all_images,
            "results": results,
            "source": "Database",
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc