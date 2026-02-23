from datetime import datetime

from sqlalchemy import DateTime, ForeignKey, Index, Integer, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from models.base import Base


class ImageFile(Base):
    __tablename__ = "ImageFiles"
    __table_args__ = (
        Index("IX_ImageFiles_AdmsProcessId", "AdmsProcessId"),
        Index("IX_ImageFiles_Name_Directory_AdmsProcessId", "Name", "Directory", "AdmsProcessId"),
        Index("IX_ImageFiles_Name_Directory_Category", "Name", "Directory", "Category"),
    )

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(255), nullable=False)
    directory: Mapped[str] = mapped_column("Directory", String(255), nullable=False)
    size: Mapped[str] = mapped_column("Size", String(10), nullable=False)
    status: Mapped[str] = mapped_column("Status", String(50), nullable=False)
    adms_process_id: Mapped[int | None] = mapped_column("AdmsProcessId", ForeignKey("AdmsProcesses.Id"))
    category: Mapped[str | None] = mapped_column("Category", String(100))
    captured_time: Mapped[datetime] = mapped_column("CapturedTime", DateTime, default=datetime.now)

    adms_process = relationship("AdmsProcess", back_populates="image_files")
    training_image_results = relationship("TrainingImageResult", back_populates="image_file")


class TrainingImageResult(Base):
    __tablename__ = "TrainingImageResults"
    __table_args__ = (
        Index("IX_TrainingImageResults_TrainingRecordId", "TrainingRecordId"),
        Index("IX_TrainingImageResults_ImageFileId", "ImageFileId"),
        Index("IX_TrainingImageResults_AdmsProcessId", "AdmsProcessId"),
        Index(
            "IX_TrainingImageResults_TrainingRecord_Labels",
            "TrainingRecordId",
            "TrueLabel",
            "PredictedLabel",
        ),
    )

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    training_record_id: Mapped[int] = mapped_column(
        "TrainingRecordId", ForeignKey("TrainingRecords.Id"), nullable=False
    )
    image_file_id: Mapped[int] = mapped_column("ImageFileId", ForeignKey("ImageFiles.Id"), nullable=False)
    true_label: Mapped[str] = mapped_column("TrueLabel", String(100), nullable=False)
    predicted_label: Mapped[str] = mapped_column("PredictedLabel", String(100), nullable=False)
    confidence: Mapped[float | None] = mapped_column("Confidence")
    status: Mapped[str] = mapped_column("Status", String(20), nullable=False)
    category: Mapped[str | None] = mapped_column("Category", String(100))
    adms_process_id: Mapped[int | None] = mapped_column("AdmsProcessId", ForeignKey("AdmsProcesses.Id"))
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime, default=datetime.now)

    training_record = relationship("TrainingRecord", back_populates="training_image_results")
    image_file = relationship("ImageFile", back_populates="training_image_results")
