from pydantic import BaseModel, Field

from dtos.training_dtos import (
    ClassifierParams,
    ColorParams,
    GeometryParams,
    NoiseParams,
    TrainingDto,
)


class PyCategorySource(BaseModel):
    label: str
    paths: list[str] = Field(default_factory=list)


class PyGeometryParams(GeometryParams):
    pass


class PyColorParams(ColorParams):
    pass


class PyNoiseParams(NoiseParams):
    pass


class PyClassifierParams(ClassifierParams):
    pass


class PyTrainingParameters(BaseModel):
    categories: list[str] = Field(default_factory=list)
    iterations: int = 20
    training_proportion: float = 0.8
    validation_proportion: float = 0.2
    test_proportion: float = 0.0
    early_stopping_patience: int = 10
    add_fft: bool = False
    gray_input: bool = False
    geometry: PyGeometryParams = Field(default_factory=PyGeometryParams)
    color: PyColorParams = Field(default_factory=PyColorParams)
    noise: PyNoiseParams = Field(default_factory=PyNoiseParams)
    classifier: PyClassifierParams = Field(default_factory=PyClassifierParams)

    @classmethod
    def from_training_dto(cls, dto: TrainingDto) -> "PyTrainingParameters":
        return cls(
            categories=dto.categories,
            iterations=dto.iterations,
            training_proportion=dto.training_proportion,
            validation_proportion=dto.validation_proportion,
            test_proportion=dto.test_proportion,
            early_stopping_patience=dto.early_stopping_patience,
            add_fft=dto.classifier.add_fft,
            gray_input=dto.classifier.gray_input,
            geometry=PyGeometryParams(**dto.geometry.model_dump()),
            color=PyColorParams(**dto.color.model_dump()),
            noise=PyNoiseParams(**dto.noise.model_dump()),
            classifier=PyClassifierParams(**dto.classifier.model_dump()),
        )


class PyClsTrainRequest(BaseModel):
    data: str = ""
    out: str
    best_model_path: str | None = None
    category_sources: list[PyCategorySource] | None = None
    img_size: int = 512
    epochs: int = 20
    batch_size: int = 16
    workers: int = 4
    backbone: str = "tf_efficientnet_b0"
    lr: float = 3e-4
    val_split: float = 0.2
    patience: int = 10
    add_fft: bool = False
    gray_input: bool = False
    geom_aug: PyGeometryParams | None = None
    color_aug: PyColorParams | None = None
    noise_aug: PyNoiseParams | None = None
    strong_aug: bool = False

    @classmethod
    def from_training_parameters(
        cls,
        params: PyTrainingParameters,
        data_path: str,
        out_path: str,
        best_model_path: str | None = None,
        category_sources: list[PyCategorySource] | None = None,
    ) -> "PyClsTrainRequest":
        return cls(
            data=data_path,
            out=out_path,
            best_model_path=best_model_path,
            category_sources=category_sources,
            img_size=params.classifier.image_width,
            epochs=params.iterations,
            batch_size=params.classifier.batch_size,
            val_split=params.validation_proportion,
            patience=params.early_stopping_patience,
            add_fft=params.add_fft,
            gray_input=params.gray_input,
            geom_aug=params.geometry,
            color_aug=params.color,
            noise_aug=params.noise,
        )


class PyTrainStartResponse(BaseModel):
    result: str = ""


class PyTrainStatusResponse(BaseModel):
    running: bool = False
    progress: float = 0.0
    current_epoch: int = 0
    total_epochs: int = 0
    current_accuracy: float = 0.0
    best_accuracy: float = 0.0
    train_loss: float = 0.0
    val_loss: float = 0.0
    best_model: str | None = None
    last_update: str | None = None


class PyTrainStopResponse(BaseModel):
    result: str = ""
    status: str | None = None
    best_model: str | None = None
    out_dir: str | None = None
    last_lines: str | None = None


class PyOnnlPackRequest(BaseModel):
    weights: str
    out_path: str | None = None
    opset: int = 13
    seal: bool = False
    aud: str | None = None


class PyOnnlPackResponse(BaseModel):
    path: str | None = None
    status: str | None = None
    error: str | None = None


class PyTrainLogResponse(BaseModel):
    log: str = ""


class PyConfusionResponse(BaseModel):
    count: int = 0
    error: str | None = None


class PyConfusionMatrixResponse(BaseModel):
    confusion: dict[str, dict[str, int]] | None = None
    matrix: list[list[int]] | None = None
    classes: list[str] | None = None
    error: str | None = None


class PyClassifyResponse(BaseModel):
    bestLabel: str | None = None
    bestScore: float = 0.0
    allScores: dict[str, float] | None = None
    error: str | None = None


class PyClassificationResult(BaseModel):
    best_label: str | None = None
    best_score: float = 0.0
    all_scores: dict[str, float] = Field(default_factory=dict)
