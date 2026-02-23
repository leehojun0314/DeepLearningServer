from datetime import datetime

from sqlalchemy import DateTime, ForeignKey, Integer, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from models.base import Base


class LogRecord(Base):
    __tablename__ = "LogRecords"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    message: Mapped[str] = mapped_column("Message", String(255), nullable=False)
    level: Mapped[str] = mapped_column("Level", String(50), nullable=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime, default=datetime.now)


class RecipeFile(Base):
    __tablename__ = "RecipeFiles"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    file_name: Mapped[str] = mapped_column("FileName", String(100), nullable=False)
    content: Mapped[str] = mapped_column("Content", nullable=False)
    file_type: Mapped[str] = mapped_column("FileType", String(50), nullable=False)
    sync_status: Mapped[str] = mapped_column("SyncStatus", nullable=False)
    adms_process_id: Mapped[int] = mapped_column(
        "AdmsProcessId", ForeignKey("AdmsProcesses.Id"), nullable=False
    )
    created_date: Mapped[datetime] = mapped_column("CreatedDate", DateTime, default=datetime.now)

    adms_process = relationship("AdmsProcess", back_populates="recipe_files")
