from passlib.context import CryptContext
from jose import JWTError, jwt
from datetime import datetime, timedelta
from typing import Optional, Dict, Any
from sqlalchemy.exc import IntegrityError
from models.user import User
from config import settings

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

class AuthService:
    def __init__(self):
        # For demo purposes, we'll just simulate database operations
        pass
    
    def verify_password(self, plain_password: str, hashed_password: str) -> bool:
        return pwd_context.verify(plain_password, hashed_password)
    
    def get_password_hash(self, password: str) -> str:
        return pwd_context.hash(password)
    
    async def register_user(self, username: str, password: str, email: str) -> Dict[str, Any]:
        # Check if user already exists
        # For demo purposes, we'll just simulate the operation
        return {"message": "User registered successfully"}
    
    async def login_user(self, username: str, password: str) -> str:
        # For demo purposes, we'll just simulate the operation
        # Create JWT token
        expire = datetime.utcnow() + timedelta(minutes=settings.jwt_access_token_expire_minutes)
        to_encode = {
            "sub": username,
            "user_id": 1,
            "exp": expire
        }
        
        encoded_jwt = jwt.encode(
            to_encode, 
            settings.jwt_secret_key, 
            algorithm=settings.jwt_algorithm
        )
        
        return encoded_jwt
