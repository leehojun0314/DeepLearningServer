from pydantic import BaseModel, Field


class NgSyncResultDto(BaseModel):
    image_size: str
    image_root: str
    total_files_scanned: int = 0
    inserted: int = 0
    skipped: int = 0
    inserted_by_category: dict[str, int] = Field(default_factory=dict)
    errors: list[str] = Field(default_factory=list)


class OkSyncResultDto(BaseModel):
    image_size: str
    image_root: str
    adms_process_id: int
    process_name: str = ""
    total_files_scanned: int = 0
    inserted: int = 0
    skipped: int = 0
    inserted_base: int = 0
    inserted_new: int = 0
    errors: list[str] = Field(default_factory=list)
