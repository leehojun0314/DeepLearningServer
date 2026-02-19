from fastapi import FastAPI, Depends, HTTPException, status, Response
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import OAuth2PasswordBearer
from sqlalchemy.orm import Session
import uvicorn
from controllers import auth_controller, model_controller, inference_controller, adms_controller, deeplearning_controller
from services.db_service import get_db
from config import settings

# Create FastAPI app
app = FastAPI(
    title="DeepLearning Server",
    description="Python-based Deep Learning Server API",
    version="1.0.0"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(auth_controller.router, prefix="/api/auth", tags=["Authentication"])
app.include_router(model_controller.router, prefix="/api/model", tags=["Model Management"])
app.include_router(inference_controller.router, prefix="/api/inference", tags=["Inference"])
app.include_router(adms_controller.router, prefix="/api/adms", tags=["ADMS"])
app.include_router(deeplearning_controller.router, prefix="/api/deeplearning", tags=["Deep Learning"])

# Health check endpoint
@app.get("/", response_model=dict)
async def root():
    return {
        "status": "OK",
        "message": "ADMS DeepLearning Server is running",
        "timestamp": "2026-02-17 03:52:32",
        "version": "1.0.0",
        "environment": "production"
    }

# Error handling
@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    return Response(
        status_code=500,
        content='{"detail": "Internal server error"}'
    )

if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host=settings.server_host,
        port=settings.server_port,
        log_level="info"
    )