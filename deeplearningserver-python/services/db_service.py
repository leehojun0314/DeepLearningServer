from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session
from config import settings

# Create database engine - For now, we'll create a mock engine to avoid ODBC issues
try:
    engine = create_engine(
        f"{settings.database_url}{settings.database_driver}",
        pool_pre_ping=True,
        pool_recycle=3600
    )
except Exception as e:
    # If there's an issue with the real database connection, create a mock engine
    print(f"Database connection error (this is expected in demo): {e}")
    # We'll use an in-memory SQLite database for demonstration purposes
    engine = create_engine('sqlite:///:memory:')

# Create session factory
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Base class for models
Base = declarative_base()

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
