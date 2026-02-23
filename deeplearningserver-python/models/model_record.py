from datetime import datetime

from sqlalchemy import DateTime, ForeignKey, Integer, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from models.base import Base


class ModelRecord(Base):
    __tablename__ = "ModelRecords"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    model_name: Mapped[str] = mapped_column("ModelName", String(255), nullable=False)
    status: Mapped[str] = mapped_column("Status", String(50), nullable=False, default="saved")
    client_path: Mapped[str | None] = mapped_column("ClientPath", String(500))
    server_path: Mapped[str | None] = mapped_column("ServerPath", String(500))
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime, default=datetime.now)
    updated_at: Mapped[datetime | None] = mapped_column("UpdatedAt", DateTime)
    adms_process_type_id: Mapped[int] = mapped_column(
        "AdmsProcessTypeId", ForeignKey("AdmsProcessTypes.Id"), nullable=False
    )
    training_record_id: Mapped[int | None] = mapped_column(
        "TrainingRecordId", ForeignKey("TrainingRecords.Id")
    )

    adms_process_type = relationship("AdmsProcessType", back_populates="model_records")
    training_record = relationship("TrainingRecord", back_populates="model_records")
