import logging
from datetime import datetime
from pathlib import Path

from fastapi import FastAPI, Response
from fastapi.middleware.cors import CORSMiddleware
import uvicorn

from controllers import adms_controller, auth_controller, deeplearning_controller, inference_controller, model_controller
from config import settings
from services.db_service import init_db
from services.mssql_db_service import MssqlDbService

log_dir = Path("logs")
log_dir.mkdir(exist_ok=True)
log_file = log_dir / f"app-{datetime.now().strftime('%Y-%m-%d')}.log"
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s - %(message)s",
    handlers=[logging.FileHandler(log_file, encoding="utf-8"), logging.StreamHandler()],
)
logger = logging.getLogger("deeplearningserver")
db_service = MssqlDbService()

# Create FastAPI app
app = FastAPI(
    title="DeepLearning Server",
    description="Python-based Deep Learning Server API",
    version="1.0.0"
)


@app.on_event("startup")
async def on_startup() -> None:
    ok, msg = init_db()
    if ok:
        logger.info(msg)
    else:
        logger.warning(msg)

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
        "timestamp": datetime.utcnow().isoformat(),
        "version": "1.0.0",
        "environment": "production",
    }

# Error handling
@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    logger.exception("Unhandled exception: %s", exc)
    try:
        await db_service.insert_log(f"Unhandled exception: {exc}", "Error")
    except Exception:
        pass
    return Response(
        status_code=500,
        content='{"detail": "Internal server error"}',
    )

if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host=settings.server_host,
        port=settings.server_port,
        log_level="info"
    )