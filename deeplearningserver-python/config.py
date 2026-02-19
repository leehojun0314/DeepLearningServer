import os
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    # Database settings
    database_url: str = "mssql+pyodbc:///?odbc_connect="
    database_driver: str = "Driver={ODBC Driver 17 for SQL Server};Server=localhost;Database=DeepLearningDB;Trusted_Connection=yes;"
    
    # Server settings
    server_port: int = 8082
    server_host: str = "0.0.0.0"
    
    # JWT settings
    jwt_secret_key: str = "your-secret-key-here"
    jwt_algorithm: str = "HS256"
    jwt_access_token_expire_minutes: int = 120
    
    # Python training server settings
    python_training_server_url: str = "http://localhost:8000"
    use_python_server: bool = False
    
    # File paths
    middle_image_path: str = ""
    large_image_path: str = ""
    model_directory: str = ""
    evaluation_model_directory: str = ""
    temp_image_directory: str = ""
    
    class Config:
        env_file = ".env"
        case_sensitive = True
        extra = "ignore"

settings = Settings()