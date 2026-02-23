from enum import Enum


class ImageSize(str, Enum):
    MIDDLE = "Middle"
    LARGE = "Large"


class TrainingStatus(str, Enum):
    LOADING = "Loading"
    RUNNING = "Running"
    COMPLETED = "Completed"
    FAILED = "Failed"
    STANBY = "Stanby"
    CANCELLED = "Cancelled"
