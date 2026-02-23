from typing import Callable

from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from models.enums import PermissionType, UserRoleType
from services.auth_service import AuthService

bearer_scheme = HTTPBearer(auto_error=False)
auth_service = AuthService()


def get_current_user(
    credentials: HTTPAuthorizationCredentials | None = Depends(bearer_scheme),
) -> dict:
    if not credentials or not credentials.credentials:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Not authenticated")
    try:
        return auth_service.verify_token(credentials.credentials)
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail=str(exc)) from exc


def require_permission(permission_type: PermissionType | str) -> Callable:
    required = permission_type.value if isinstance(permission_type, PermissionType) else permission_type

    def dependency(user: dict = Depends(get_current_user)) -> dict:
        roles = set(user.get("roles", []))
        permissions = set(user.get("permissions", []))
        if "SuperAdmin" in roles or required in permissions:
            return user
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Permission denied")

    return dependency


def require_role(*role_types: UserRoleType | str) -> Callable:
    expected = {r.value if isinstance(r, UserRoleType) else r for r in role_types}

    def dependency(user: dict = Depends(get_current_user)) -> dict:
        roles = set(user.get("roles", []))
        if "SuperAdmin" in roles or roles.intersection(expected):
            return user
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Role denied")

    return dependency
