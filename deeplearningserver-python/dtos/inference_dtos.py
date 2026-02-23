from pydantic import BaseModel


class InferenceDto(BaseModel):
    model_path: str
    image_path: str


class MultiInferenceDto(BaseModel):
    model_path: str
    image_paths: list[str]


class LabelProbabilityDto(BaseModel):
    label: str
    probability: float


class ClassificationResultDto(BaseModel):
    best_label: str | None = None
    best_probability: float = 0.0
    label_probabilities: list[LabelProbabilityDto] = []
