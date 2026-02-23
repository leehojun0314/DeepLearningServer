from datetime import datetime, timedelta
from typing import Any

from jose import JWTError, jwt
from passlib.context import CryptContext
from sqlalchemy.orm import Session

from config import settings
from models import Permission, Role, RolePermission, User, UserRole

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")


class AuthService:
    def verify_password(self, plain_password: str, hashed_password: str) -> bool:
        return pwd_context.verify(plain_password, hashed_password)

    def get_password_hash(self, password: str) -> str:
        return pwd_context.hash(password)

    async def register_user(self, db: Session, username: str, password: str, email: str) -> dict[str, Any]:
        existing = (
            db.query(User)
            .filter((User.username == username) | (User.email == email))
            .first()
        )
        if existing:
            raise ValueError("Username or Email already exists")

        user = User(
            username=username,
            email=email,
            password_hash=self.get_password_hash(password),
            is_active=True,
        )
        db.add(user)
        db.flush()

        operator_role = db.query(Role).filter(Role.name == "Operator").first()
        if operator_role:
            db.add(UserRole(user_id=user.id, role_id=operator_role.id))
        db.commit()
        return {"message": "User registered successfully"}

    async def login_user(self, db: Session, username: str, password: str) -> str:
        user = db.query(User).filter(User.username == username).first()
        if not user or not self.verify_password(password, user.password_hash):
            raise ValueError("Invalid credentials")

        role_names = [
            row.name
            for row in db.query(Role)
            .join(UserRole, UserRole.role_id == Role.id)
            .filter(UserRole.user_id == user.id)
            .all()
        ]
        permission_names = [
            row.name
            for row in db.query(Permission)
            .join(RolePermission, RolePermission.permission_id == Permission.id)
            .join(Role, Role.id == RolePermission.role_id)
            .filter(Role.name.in_(role_names))
            .all()
        ]

        expire = datetime.utcnow() + timedelta(minutes=settings.jwt_access_token_expire_minutes)
        payload = {
            "sub": user.username,
            "UserId": str(user.id),
            "roles": role_names,
            "permissions": permission_names,
            "exp": expire,
        }
        return jwt.encode(payload, settings.jwt_secret_key, algorithm=settings.jwt_algorithm)

    def verify_token(self, token: str) -> dict[str, Any]:
        try:
            return jwt.decode(token, settings.jwt_secret_key, algorithms=[settings.jwt_algorithm])
        except JWTError as exc:
            raise ValueError("Invalid token") from exc
