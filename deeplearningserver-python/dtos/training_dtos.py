from pydantic import BaseModel, Field

from dtos.common import ImageSize


class GeometryParams(BaseModel):
    max_rotation: float = 0.0
    max_vertical_shift: int = 0
    max_horizontal_shift: int = 0
    min_scale: float = 1.0
    max_scale: float = 1.0
    max_vertical_shear: float = 0.0
    max_horizontal_shear: float = 0.0
    vertical_flip: bool = False
    horizontal_flip: bool = False


class ColorParams(BaseModel):
    max_brightness_offset: float = 0.0
    min_contrast_gain: float = 1.0
    max_contrast_gain: float = 1.0
    min_gamma: float = 1.0
    max_gamma: float = 1.0
    hue_offset: float = 0.0
    min_saturation_gain: float = 1.0
    max_saturation_gain: float = 1.0


class NoiseParams(BaseModel):
    min_gaussian_deviation: float = 0.0
    max_gaussian_deviation: float = 0.0
    min_speckle_deviation: float = 0.0
    max_speckle_deviation: float = 0.0
    min_salt_pepper_noise: float = 0.0
    max_salt_pepper_noise: float = 0.0


class ClassifierParams(BaseModel):
    classifier_capacity: str = "Normal"
    image_width: int = 512
    image_height: int = 512
    image_cache_size: int = 0
    image_channels: int = 3
    add_fft: bool = False
    gray_input: bool = False
    use_pretrained_model: bool = False
    compute_heat_map: bool = True
    enable_histogram_equalization: bool = False
    batch_size: int = 32
    enable_deterministic_training: bool = False


class TrainingDto(BaseModel):
    adms_process_ids: list[int]
    image_size: ImageSize
    categories: list[str] = Field(default_factory=list)
    is_default_model: bool = False
    client_model_destination: str = "Middle"
    training_proportion: float = 1.0
    validation_proportion: float = 0.0
    test_proportion: float = 0.0
    iterations: int = 50
    early_stopping_patience: int = 10
    geometry: GeometryParams = Field(default_factory=GeometryParams)
    color: ColorParams = Field(default_factory=ColorParams)
    noise: NoiseParams = Field(default_factory=NoiseParams)
    classifier: ClassifierParams = Field(default_factory=ClassifierParams)
