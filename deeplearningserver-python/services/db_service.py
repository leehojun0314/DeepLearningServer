from urllib.parse import quote_plus

from sqlalchemy import create_engine
from sqlalchemy.exc import SQLAlchemyError
from sqlalchemy.orm import Session, sessionmaker

from config import settings
from models import Base  # noqa: F401 - model registration side effect


def _build_database_url() -> str:
    if settings.database_url.endswith("odbc_connect="):
        return f"{settings.database_url}{quote_plus(settings.database_driver)}"
    return settings.database_url


engine = create_engine(
    _build_database_url(),
    pool_pre_ping=True,
    pool_recycle=3600,
)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    db: Session = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def init_db() -> tuple[bool, str]:
    try:
        # Create tables only when they do not exist.
        Base.metadata.create_all(bind=engine)
        return True, "Database initialized"
    except SQLAlchemyError as exc:
        return False, f"Database initialization skipped: {exc}"
