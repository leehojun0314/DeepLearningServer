USE DL_SERVER;

-- 로그인 계정이 있는지 확인 후 없으면 생성
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'ADMS')
BEGIN
    CREATE LOGIN ADMS WITH PASSWORD = 'developer123@';
    PRINT 'Login created';
END
ELSE
BEGIN
    PRINT 'Login already exists';
END

-- 데이터베이스 사용자 확인 후 없으면 생성
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'ADMS')
BEGIN
    CREATE USER ADMS FOR LOGIN ADMS;
    ALTER ROLE db_owner ADD MEMBER ADMS;
    PRINT 'User created and added to db_owner role';
END
ELSE
BEGIN
    PRINT 'User already exists';
END

-- 역할(Role) 테이블 초기화
INSERT INTO Roles (Name) 
SELECT 'ServiceEngineer' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'ServiceEngineer');

INSERT INTO Roles (Name) 
SELECT 'Manager' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Manager');

INSERT INTO Roles (Name) 
SELECT 'HWEngineer' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'HWEngineer');

INSERT INTO Roles (Name) 
SELECT 'PROCEngineer' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'PROCEngineer');

INSERT INTO Roles (Name) 
SELECT 'Operator' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Operator');

-- 권한(Permission) 테이블 초기화
INSERT INTO Permissions (Name) 
SELECT 'RunModel' WHERE NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'RunModel');

INSERT INTO Permissions (Name) 
SELECT 'ViewLogs' WHERE NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'ViewLogs');

INSERT INTO Permissions (Name) 
SELECT 'ManageUsers' WHERE NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'ManageUsers');

INSERT INTO Permissions (Name) 
SELECT 'DeployModel' WHERE NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'DeployModel');

INSERT INTO Permissions (Name) 
SELECT 'TrainModel' WHERE NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'TrainModel');

-- 기본 역할에 기본 권한 추가 (Manager -> RunModel)
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r, Permissions p
WHERE r.Name = 'Manager' AND p.Name = 'RunModel'
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp 
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
);

-- ADMIN 계정 생성 (EnableAdminSeed 설정이 활성화된 경우)
-- (Admin 계정이 없으면 생성)
INSERT INTO Users (Username, PasswordHash, IsActive)
SELECT 'ADMS', '$2a$11$9iDxqe19.mhiuPQnZECh1O83WbpUpYgiWdtOYQR0JhZvt3tMCsWFK', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ADMIN');

-- ADMIN 역할 확인 및 생성
INSERT INTO Roles (Name)
SELECT 'ServiceEngineer' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'ServiceEngineer');

-- ADMIN 계정에 ServiceEngineer 역할 할당
INSERT INTO UserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM Users u, Roles r
WHERE u.Username = 'ADMIN' AND r.Name = 'ServiceEngineer'
AND NOT EXISTS (
    SELECT 1 FROM UserRoles ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
);

-- ADMIN 계정에 모든 권한 부여
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r, Permissions p
WHERE r.Name = 'ServiceEngineer'
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
);
