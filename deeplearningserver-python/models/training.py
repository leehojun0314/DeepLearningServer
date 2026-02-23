from datetime import datetime

from sqlalchemy import Boolean, DateTime, Float, ForeignKey, Integer, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from models.base import Base


class TrainingRecord(Base):
    __tablename__ = "TrainingRecords"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    image_size: Mapped[int] = mapped_column("ImageSize", Integer, nullable=False)
    created_time: Mapped[datetime] = mapped_column("CreatedTime", DateTime, default=datetime.now)
    start_time: Mapped[datetime | None] = mapped_column("StartTime", DateTime)
    end_time: Mapped[datetime | None] = mapped_column("EndTime", DateTime)
    has_pretrained_model: Mapped[bool] = mapped_column(
        "HasPretrainedModel", Boolean, nullable=False, default=False
    )
    accuracy: Mapped[float | None] = mapped_column("Accuracy", Float)
    loss: Mapped[float | None] = mapped_column("Loss", Float)
    progress: Mapped[float | None] = mapped_column("Progress", Float)
    best_iteration: Mapped[int | None] = mapped_column("BestIteration", Integer)
    status: Mapped[str | None] = mapped_column("Status", String(50))

    max_rotation: Mapped[float] = mapped_column("MaxRotation", Float, default=0.0)
    max_vertical_shift: Mapped[float] = mapped_column("MaxVerticalShift", Float, default=0.0)
    max_horizontal_shift: Mapped[float] = mapped_column("MaxHorizontalShift", Float, default=0.0)
    min_scale: Mapped[float] = mapped_column("MinScale", Float, default=1.0)
    max_scale: Mapped[float] = mapped_column("MaxScale", Float, default=1.0)
    max_vertical_shear: Mapped[float] = mapped_column("MaxVerticalShear", Float, default=0.0)
    max_horizontal_shear: Mapped[float] = mapped_column("MaxHorizontalShear", Float, default=0.0)
    vertical_flip: Mapped[bool] = mapped_column("VerticalFlip", Boolean, default=False)
    horizontal_flip: Mapped[bool] = mapped_column("HorizontalFlip", Boolean, default=False)
    max_brightness_offset: Mapped[float] = mapped_column("MaxBrightnessOffset", Float, default=0.0)
    max_contrast_gain: Mapped[float] = mapped_column("MaxContrastGain", Float, default=1.0)
    min_contrast_gain: Mapped[float] = mapped_column("MinContrastGain", Float, default=1.0)
    max_gamma: Mapped[float] = mapped_column("MaxGamma", Float, default=1.0)
    min_gamma: Mapped[float] = mapped_column("MinGamma", Float, default=1.0)
    hue_offset: Mapped[float] = mapped_column("HueOffset", Float, default=0.0)
    max_saturation_gain: Mapped[float] = mapped_column("MaxSaturationGain", Float, default=1.0)
    min_saturation_gain: Mapped[float] = mapped_column("MinSaturationGain", Float, default=1.0)
    max_gaussian_deviation: Mapped[float] = mapped_column("MaxGaussianDeviation", Float, default=0.0)
    min_gaussian_deviation: Mapped[float] = mapped_column("MinGaussianDeviation", Float, default=0.0)
    max_speckle_deviation: Mapped[float] = mapped_column("MaxSpeckleDeviation", Float, default=0.0)
    min_speckle_deviation: Mapped[float] = mapped_column("MinSpeckleDeviation", Float, default=0.0)
    max_salt_pepper_noise: Mapped[float] = mapped_column("MaxSaltPepperNoise", Float, default=0.0)
    min_salt_pepper_noise: Mapped[float] = mapped_column("MinSaltPepperNoise", Float, default=0.0)
    classifier_capacity: Mapped[int] = mapped_column("ClassifierCapacity", Integer, default=1)
    image_cache_size: Mapped[int] = mapped_column("ImageCacheSize", Integer, default=0)
    image_width: Mapped[int] = mapped_column("ImageWidth", Integer, default=512)
    image_height: Mapped[int] = mapped_column("ImageHeight", Integer, default=512)
    image_channels: Mapped[int] = mapped_column("ImageChannels", Integer, default=3)
    use_pretrained_model: Mapped[bool] = mapped_column("UsePretrainedModel", Boolean, default=False)
    compute_heat_map: Mapped[bool] = mapped_column("ComputeHeatMap", Boolean, default=False)
    enable_histogram_equalization: Mapped[bool] = mapped_column(
        "EnableHistogramEqualization", Boolean, default=False
    )
    batch_size: Mapped[int] = mapped_column("BatchSize", Integer, default=32)

    labels = relationship("Label", back_populates="training_record", cascade="all, delete-orphan")
    progress_entries = relationship(
        "ProgressEntry", back_populates="training_record", cascade="all, delete-orphan"
    )
    training_adms_processes = relationship(
        "TrainingAdmsProcess", back_populates="training_record", cascade="all, delete-orphan"
    )
    model_records = relationship("ModelRecord", back_populates="training_record")
    training_image_results = relationship(
        "TrainingImageResult", back_populates="training_record", cascade="all, delete-orphan"
    )


class TrainingAdmsProcess(Base):
    __tablename__ = "TrainingAdmsProcesses"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    training_record_id: Mapped[int] = mapped_column(
        "TrainingRecordId", ForeignKey("TrainingRecords.Id"), nullable=False
    )
    adms_process_id: Mapped[int] = mapped_column(
        "AdmsProcessId", ForeignKey("AdmsProcesses.Id"), nullable=False
    )

    training_record = relationship("TrainingRecord", back_populates="training_adms_processes")
    adms_process = relationship("AdmsProcess", back_populates="training_adms_processes")


class Label(Base):
    __tablename__ = "Labels"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(100), nullable=False)
    accuracy: Mapped[float | None] = mapped_column("Accuracy", Float)
    training_record_id: Mapped[int] = mapped_column(
        "TrainingRecordId", ForeignKey("TrainingRecords.Id"), nullable=False
    )

    training_record = relationship("TrainingRecord", back_populates="labels")


class ProgressEntry(Base):
    __tablename__ = "ProgressEntries"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    is_training: Mapped[bool] = mapped_column("IsTraining", Boolean, nullable=False, default=True)
    progress: Mapped[float] = mapped_column("Progress", Float, nullable=False, default=0.0)
    best_iteration: Mapped[float] = mapped_column("BestIteration", Float, nullable=False, default=0.0)
    accuracy: Mapped[float | None] = mapped_column("Accuracy", Float)
    validation_accuracy: Mapped[float | None] = mapped_column("ValidationAccuracy", Float)
    validation_error: Mapped[float | None] = mapped_column("ValidationError", Float)
    start_time: Mapped[datetime] = mapped_column("StartTime", DateTime, default=datetime.now)
    end_time: Mapped[datetime | None] = mapped_column("EndTime", DateTime)
    duration: Mapped[float | None] = mapped_column("Duration", Float)
    training_record_id: Mapped[int] = mapped_column(
        "TrainingRecordId", ForeignKey("TrainingRecords.Id"), nullable=False
    )

    training_record = relationship("TrainingRecord", back_populates="progress_entries")
