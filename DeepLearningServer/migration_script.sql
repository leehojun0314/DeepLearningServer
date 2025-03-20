IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Adms] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NULL,
    [LocalIp] nvarchar(100) NOT NULL,
    [MacAddress] nvarchar(100) NOT NULL,
    [CpuId] nvarchar(100) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime NOT NULL,
    [UpdatedAt] datetime NOT NULL,
    CONSTRAINT [PK_Adms] PRIMARY KEY ([Id])
);

CREATE TABLE [LogRecords] (
    [Id] int NOT NULL IDENTITY,
    [Message] nvarchar(255) NOT NULL,
    [Level] nvarchar(50) NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_LogRecords] PRIMARY KEY ([Id])
);

CREATE TABLE [Processes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [CreatedAt] datetime NOT NULL,
    [UpdatedAt] datetime NOT NULL,
    CONSTRAINT [PK_Processes] PRIMARY KEY ([Id])
);

CREATE TABLE [TrainingRecords] (
    [Id] int NOT NULL IDENTITY,
    [ImageSize] int NOT NULL,
    [CreatedTime] datetime2 NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [ModelName] nvarchar(max) NULL,
    [ModelPath] nvarchar(max) NULL,
    [Accuracy] float NULL,
    [Loss] float NULL,
    [Progress] real NULL,
    [BestIteration] int NULL,
    [Status] nvarchar(50) NULL,
    [MaxRotation] real NOT NULL,
    [MaxVerticalShift] real NOT NULL,
    [MaxHorizontalShift] real NOT NULL,
    [MinScale] real NOT NULL,
    [MaxScale] real NOT NULL,
    [MaxVerticalShear] real NOT NULL,
    [MaxHorizontalShear] real NOT NULL,
    [VerticalFlip] bit NOT NULL,
    [HorizontalFlip] bit NOT NULL,
    [MaxBrightnessOffset] real NOT NULL,
    [MaxContrastGain] real NOT NULL,
    [MinContrastGain] real NOT NULL,
    [MaxGamma] real NOT NULL,
    [MinGamma] real NOT NULL,
    [HueOffset] real NOT NULL,
    [MaxSaturationGain] real NOT NULL,
    [MinSaturationGain] real NOT NULL,
    [MaxGaussianDeviation] real NOT NULL,
    [MinGaussianDeviation] real NOT NULL,
    [MaxSpeckleDeviation] real NOT NULL,
    [MinSpeckleDeviation] real NOT NULL,
    [MaxSaltPepperNoise] real NOT NULL,
    [MinSaltPepperNoise] real NOT NULL,
    [ClassifierCapacity] int NOT NULL,
    [ImageCacheSize] int NOT NULL,
    [ImageWidth] int NOT NULL,
    [ImageHeight] int NOT NULL,
    [ImageChannels] int NOT NULL,
    [UsePretrainedModel] bit NOT NULL,
    [ComputeHeatMap] bit NOT NULL,
    [EnableHistogramEqualization] bit NOT NULL,
    [BatchSize] int NOT NULL,
    CONSTRAINT [PK_TrainingRecords] PRIMARY KEY ([Id])
);

CREATE TABLE [AdmsProcesses] (
    [Id] int NOT NULL IDENTITY,
    [AdmsId] int NOT NULL,
    [ProcessId] int NOT NULL,
    CONSTRAINT [PK_AdmsProcesses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdmsProcesses_Adms_AdmsId] FOREIGN KEY ([AdmsId]) REFERENCES [Adms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdmsProcesses_Processes_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [Processes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Labels] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Accuracy] real NULL,
    [TrainingRecordId] int NOT NULL,
    CONSTRAINT [PK_Labels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Labels_TrainingRecords_TrainingRecordId] FOREIGN KEY ([TrainingRecordId]) REFERENCES [TrainingRecords] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProgressEntries] (
    [Id] int NOT NULL IDENTITY,
    [IsTraining] bit NOT NULL,
    [Progress] float NOT NULL,
    [BestIteration] float NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [TrainingRecordId] int NOT NULL,
    CONSTRAINT [PK_ProgressEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProgressEntries_TrainingRecords_TrainingRecordId] FOREIGN KEY ([TrainingRecordId]) REFERENCES [TrainingRecords] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AdmsProcessType] (
    [Id] int NOT NULL IDENTITY,
    [AdmsProcessId] int NOT NULL,
    [Type] nvarchar(10) NOT NULL,
    [LastSyncDate] datetime NULL,
    [IsTrainned] bit NOT NULL,
    [IsCategorized] bit NOT NULL,
    CONSTRAINT [PK_AdmsProcessType] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdmsProcessType_AdmsProcesses_AdmsProcessId] FOREIGN KEY ([AdmsProcessId]) REFERENCES [AdmsProcesses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ImageFiles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [Directory] nvarchar(255) NOT NULL,
    [Size] nvarchar(10) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [AdmsProcessId] int NOT NULL,
    [CapturedTime] datetime NOT NULL,
    CONSTRAINT [PK_ImageFiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImageFiles_AdmsProcesses_AdmsProcessId] FOREIGN KEY ([AdmsProcessId]) REFERENCES [AdmsProcesses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RecipeFiles] (
    [Id] int NOT NULL IDENTITY,
    [FileName] nvarchar(100) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [FileType] nvarchar(50) NOT NULL,
    [AdmsProcessId] int NOT NULL,
    [LastModified] datetime NOT NULL,
    CONSTRAINT [PK_RecipeFiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecipeFiles_AdmsProcesses_AdmsProcessId] FOREIGN KEY ([AdmsProcessId]) REFERENCES [AdmsProcesses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TrainingAdmsProcess] (
    [Id] int NOT NULL IDENTITY,
    [TrainingRecordId] int NOT NULL,
    [AdmsProcessId] int NOT NULL,
    CONSTRAINT [PK_TrainingAdmsProcess] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrainingAdmsProcess_AdmsProcesses_AdmsProcessId] FOREIGN KEY ([AdmsProcessId]) REFERENCES [AdmsProcesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TrainingAdmsProcess_TrainingRecords_TrainingRecordId] FOREIGN KEY ([TrainingRecordId]) REFERENCES [TrainingRecords] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AdmsProcesses_AdmsId] ON [AdmsProcesses] ([AdmsId]);

CREATE INDEX [IX_AdmsProcesses_ProcessId] ON [AdmsProcesses] ([ProcessId]);

CREATE INDEX [IX_AdmsProcessTypes_AdmsProcessId] ON [AdmsProcessType] ([AdmsProcessId]);

CREATE INDEX [IX_ImageFiles_AdmsProcessId] ON [ImageFiles] ([AdmsProcessId]);

CREATE INDEX [IX_Labels_TrainingRecordId] ON [Labels] ([TrainingRecordId]);

CREATE INDEX [IX_ProgressEntries_TrainingRecordId] ON [ProgressEntries] ([TrainingRecordId]);

CREATE INDEX [IX_RecipeFiles_AdmsProcessId] ON [RecipeFiles] ([AdmsProcessId]);

CREATE INDEX [IX_TrainingAdmsProcesses_AdmsProcessId] ON [TrainingAdmsProcess] ([AdmsProcessId]);

CREATE INDEX [IX_TrainingAdmsProcesses_TrainingRecordId] ON [TrainingAdmsProcess] ([TrainingRecordId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250226015203_Init', N'9.0.1');

ALTER TABLE [TrainingRecords] ADD [HasPretrainedModel] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [ProgressEntries] ADD [Accuracy] real NOT NULL DEFAULT CAST(0 AS real);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250228015624_AddAccuracy', N'9.0.1');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProgressEntries]') AND [c].[name] = N'Accuracy');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [ProgressEntries] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [ProgressEntries] ALTER COLUMN [Accuracy] real NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250228065451_NullableAccuracy', N'9.0.1');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingRecords]') AND [c].[name] = N'Accuracy');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TrainingRecords] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [TrainingRecords] ALTER COLUMN [Accuracy] real NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250228081923_Change2Float', N'9.0.1');

CREATE TABLE [ModelRecord] (
    [Id] int NOT NULL IDENTITY,
    [ModelName] nvarchar(255) NOT NULL,
    [AdmsProcessTypeId] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ClientPath] nvarchar(500) NULL,
    [ServerPath] nvarchar(500) NULL,
    [CreatedAt] datetime NOT NULL,
    [UpdatedAt] datetime NULL,
    CONSTRAINT [PK_ModelRecord] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ModelRecord_AdmsProcessType_AdmsProcessTypeId] FOREIGN KEY ([AdmsProcessTypeId]) REFERENCES [AdmsProcessType] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ModelRecords_AdmsProcessTypeId] ON [ModelRecord] ([AdmsProcessTypeId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250304093426_AddModelRecord', N'9.0.1');

ALTER TABLE [AdmsProcessType] DROP CONSTRAINT [FK_AdmsProcessType_AdmsProcesses_AdmsProcessId];

ALTER TABLE [ModelRecord] DROP CONSTRAINT [FK_ModelRecord_AdmsProcessType_AdmsProcessTypeId];

ALTER TABLE [ModelRecord] DROP CONSTRAINT [PK_ModelRecord];

ALTER TABLE [AdmsProcessType] DROP CONSTRAINT [PK_AdmsProcessType];

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingRecords]') AND [c].[name] = N'ModelName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [TrainingRecords] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [TrainingRecords] DROP COLUMN [ModelName];

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingRecords]') AND [c].[name] = N'ModelPath');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [TrainingRecords] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [TrainingRecords] DROP COLUMN [ModelPath];

EXEC sp_rename N'[ModelRecord]', N'ModelRecords', 'OBJECT';

EXEC sp_rename N'[AdmsProcessType]', N'AdmsProcessTypes', 'OBJECT';

ALTER TABLE [ModelRecords] ADD [TrainingRecordId] int NULL;

ALTER TABLE [ModelRecords] ADD CONSTRAINT [PK_ModelRecords] PRIMARY KEY ([Id]);

ALTER TABLE [AdmsProcessTypes] ADD CONSTRAINT [PK_AdmsProcessTypes] PRIMARY KEY ([Id]);

CREATE INDEX [IX_ModelRecords_TrainingRecordId] ON [ModelRecords] ([TrainingRecordId]);

ALTER TABLE [AdmsProcessTypes] ADD CONSTRAINT [FK_AdmsProcessTypes_AdmsProcesses_AdmsProcessId] FOREIGN KEY ([AdmsProcessId]) REFERENCES [AdmsProcesses] ([Id]) ON DELETE CASCADE;

ALTER TABLE [ModelRecords] ADD CONSTRAINT [FK_ModelRecords_AdmsProcessTypes_AdmsProcessTypeId] FOREIGN KEY ([AdmsProcessTypeId]) REFERENCES [AdmsProcessTypes] ([Id]) ON DELETE CASCADE;

ALTER TABLE [ModelRecords] ADD CONSTRAINT [FK_ModelRecords_TrainingRecords_TrainingRecordId] FOREIGN KEY ([TrainingRecordId]) REFERENCES [TrainingRecords] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250304170932_AddModelRecord2', N'9.0.1');

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250306054003_AddUser', N'9.0.1');

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Email');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(100) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250306152729_20250307', N'9.0.1');

CREATE TABLE [PwdResetRequests] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RequestedAt] datetime NOT NULL,
    [IsUsed] bit NOT NULL,
    CONSTRAINT [PK_PwdResetRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PwdResetRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PwdResetRequests_UserId] ON [PwdResetRequests] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250313142932_PwdResetRequest', N'9.0.1');

COMMIT;
GO

