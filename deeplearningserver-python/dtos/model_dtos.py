from pydantic import BaseModel


class ModelInfoDto(BaseModel):
    file_name: str
    full_path: str
    relative_path: str
    size: str
    type: str | None = None
    adms_name: str | None = None
    process_id: str
    file_size_bytes: int
    file_size_formatted: str
    created_date: str
    modified_date: str


class ModelListRequestDto(BaseModel):
    size: str | None = None
    type: str | None = None
    adms_name: str | None = None
    process_id: str | None = None


class MigrationDto(BaseModel):
    old_models_path: str
    new_models_path: str
    project_dir: str


class CopyFileRequest(BaseModel):
    source_path: str
    destination_path: str
    move: bool = False


class SendRemoteRequest(BaseModel):
    local_path: str
    remote_ip: str
    remote_destination_path: str
