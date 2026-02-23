from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from sqlalchemy.orm import Session

from services.auth_service import AuthService
from services.db_service import get_db

router = APIRouter()


class LoginRequest(BaseModel):
    username: str
    password: str


class RegisterRequest(BaseModel):
    username: str
    password: str
    email: str


@router.post("/register", response_model=dict)
async def register(
    request: RegisterRequest,
    db: Session = Depends(get_db),
):
    auth_service = AuthService()
    try:
        return await auth_service.register_user(db, request.username, request.password, request.email)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.post("/login", response_model=dict)
async def login(
    request: LoginRequest,
    db: Session = Depends(get_db),
):
    auth_service = AuthService()
    try:
        token = await auth_service.login_user(db, request.username, request.password)
        return {"token": token}
    except ValueError as exc:
        raise HTTPException(status_code=401, detail=str(exc)) from exc