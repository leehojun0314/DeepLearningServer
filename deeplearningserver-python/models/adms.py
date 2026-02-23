from datetime import datetime

from sqlalchemy import Boolean, DateTime, ForeignKey, Integer, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from models.base import Base


class Adm(Base):
    __tablename__ = "Adms"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str | None] = mapped_column("Name", String(100))
    local_ip: Mapped[str] = mapped_column("LocalIp", String(100), nullable=False)
    mac_address: Mapped[str] = mapped_column("MacAddress", String(100), nullable=False)
    cpu_id: Mapped[str] = mapped_column("CpuId", String(100), nullable=False)
    status: Mapped[str] = mapped_column("Status", String(50), nullable=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime, default=datetime.now)
    updated_at: Mapped[datetime] = mapped_column("UpdatedAt", DateTime, default=datetime.now)

    adms_processes = relationship("AdmsProcess", back_populates="adms")


class Process(Base):
    __tablename__ = "Processes"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(100), nullable=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime, default=datetime.now)
    updated_at: Mapped[datetime] = mapped_column("UpdatedAt", DateTime, default=datetime.now)

    adms_processes = relationship("AdmsProcess", back_populates="process")


class AdmsProcess(Base):
    __tablename__ = "AdmsProcesses"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    adms_id: Mapped[int] = mapped_column("AdmsId", ForeignKey("Adms.Id"), nullable=False)
    process_id: Mapped[int] = mapped_column("ProcessId", ForeignKey("Processes.Id"), nullable=False)

    adms = relationship("Adm", back_populates="adms_processes")
    process = relationship("Process", back_populates="adms_processes")
    recipe_files = relationship("RecipeFile", back_populates="adms_process")
    image_files = relationship("ImageFile", back_populates="adms_process")
    adms_process_types = relationship("AdmsProcessType", back_populates="adms_process")
    training_adms_processes = relationship("TrainingAdmsProcess", back_populates="adms_process")


class AdmsProcessType(Base):
    __tablename__ = "AdmsProcessTypes"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    adms_process_id: Mapped[int] = mapped_column(
        "AdmsProcessId", ForeignKey("AdmsProcesses.Id"), nullable=False
    )
    type: Mapped[str] = mapped_column("Type", String(10), nullable=False)
    last_sync_date: Mapped[datetime | None] = mapped_column("LastSyncDate", DateTime)
    is_trainned: Mapped[bool] = mapped_column("IsTrainned", Boolean, nullable=False, default=False)
    is_categorized: Mapped[bool] = mapped_column(
        "IsCategorized", Boolean, nullable=False, default=False
    )

    adms_process = relationship("AdmsProcess", back_populates="adms_process_types")
    model_records = relationship("ModelRecord", back_populates="adms_process_type")
