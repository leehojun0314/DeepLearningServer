from enum import Enum


class TrainingStatus(str, Enum):
    LOADING = "Loading"
    RUNNING = "Running"
    COMPLETED = "Completed"
    FAILED = "Failed"
    STANBY = "Stanby"
    CANCELLED = "Cancelled"


class ImageSize(str, Enum):
    MIDDLE = "Middle"
    LARGE = "Large"


class PermissionType(str, Enum):
    RUN_MODEL = "RunModel"
    VIEW_LOGS = "ViewLogs"
    MANAGE_USERS = "ManageUsers"
    DEPLOY_MODEL = "DeployModel"
    TRAIN_MODEL = "TrainModel"


class UserRoleType(str, Enum):
    SERVICE_ENGINEER = "ServiceEngineer"
    MANAGER = "Manager"
    HW_ENGINEER = "HWEngineer"
    PROC_ENGINEER = "PROCEngineer"
    OPERATOR = "Operator"


class LogLevel(str, Enum):
    TRACE = "Trace"
    DEBUG = "Debug"
    INFORMATION = "Information"
    WARNING = "Warning"
    ERROR = "Error"
    CRITICAL = "Critical"
    NONE = "None"
