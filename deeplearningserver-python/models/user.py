from datetime import datetime

from sqlalchemy import Boolean, DateTime, ForeignKey, Integer, PrimaryKeyConstraint, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from models.base import Base


class User(Base):
    __tablename__ = "Users"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    username: Mapped[str] = mapped_column("Username", String(100), nullable=False)
    password_hash: Mapped[str] = mapped_column("PasswordHash", String, nullable=False)
    email: Mapped[str | None] = mapped_column("Email", String(100))
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True, nullable=False)

    user_roles = relationship("UserRole", back_populates="user", cascade="all, delete-orphan")
    pwd_reset_requests = relationship(
        "PwdResetRequest", back_populates="user", cascade="all, delete-orphan"
    )


class Role(Base):
    __tablename__ = "Roles"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(100), nullable=False)

    user_roles = relationship("UserRole", back_populates="role", cascade="all, delete-orphan")
    role_permissions = relationship(
        "RolePermission", back_populates="role", cascade="all, delete-orphan"
    )


class UserRole(Base):
    __tablename__ = "UserRoles"
    __table_args__ = (PrimaryKeyConstraint("UserId", "RoleId", name="PK_UserRoles"),)

    user_id: Mapped[int] = mapped_column("UserId", ForeignKey("Users.Id"), nullable=False)
    role_id: Mapped[int] = mapped_column("RoleId", ForeignKey("Roles.Id"), nullable=False)

    user = relationship("User", back_populates="user_roles")
    role = relationship("Role", back_populates="user_roles")


class Permission(Base):
    __tablename__ = "Permissions"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(100), nullable=False)

    role_permissions = relationship(
        "RolePermission", back_populates="permission", cascade="all, delete-orphan"
    )


class RolePermission(Base):
    __tablename__ = "RolePermissions"
    __table_args__ = (PrimaryKeyConstraint("RoleId", "PermissionId", name="PK_RolePermissions"),)

    role_id: Mapped[int] = mapped_column("RoleId", ForeignKey("Roles.Id"), nullable=False)
    permission_id: Mapped[int] = mapped_column(
        "PermissionId", ForeignKey("Permissions.Id"), nullable=False
    )

    role = relationship("Role", back_populates="role_permissions")
    permission = relationship("Permission", back_populates="role_permissions")


class PwdResetRequest(Base):
    __tablename__ = "PwdResetRequests"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    user_id: Mapped[int] = mapped_column("UserId", ForeignKey("Users.Id"), nullable=False)
    requested_at: Mapped[datetime] = mapped_column("RequestedAt", DateTime, default=datetime.now)
    is_used: Mapped[bool] = mapped_column("IsUsed", Boolean, default=False, nullable=False)

    user = relationship("User", back_populates="pwd_reset_requests")