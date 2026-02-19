from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel
from typing import Dict
from services.auth_service import AuthService
from config import settings

router = APIRouter()

class LoginRequest(BaseModel):
    username: str
    password: str

class RegisterRequest(BaseModel):
    username: str
    password: str
    email: str

@router.post("/register", response_model=Dict[str, str])
async def register(request: RegisterRequest, auth_service: AuthService = Depends()):
    try:
        result = await auth_service.register_user(request.username, request.password, request.email)
        return {"message": "User registered successfully"}
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@router.post("/login", response_model=Dict[str, str])
async def login(request: LoginRequest, auth_service: AuthService = Depends()):
    try:
        token = await auth_service.login_user(request.username, request.password)
        return {"token": token}
    except Exception as e:
        raise HTTPException(status_code=401, detail=str(e))