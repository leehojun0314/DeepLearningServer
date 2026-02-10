-- ============================================================
-- ADM(ADMS) Entry Delete Script
-- Usage: Set @AdmId to the target ADM Id, then execute
-- Target DB: DL_SERVER
--
-- Delete order (reverse FK dependency):
--   1. ConfusionMatrixImages (FK -> ImageFile)
--   2. TrainingImageResults  (FK -> ImageFile / nullable FK -> AdmsProcess)
--   3. ModelRecords          (FK -> AdmsProcessType)
--   4. AdmsProcessTypes      (FK -> AdmsProcess)
--   5. RecipeFiles           (FK -> AdmsProcess)
--   6. TrainingAdmsProcesses (FK -> AdmsProcess)
--   7. ImageFiles            (nullable FK -> AdmsProcess)
--   8. AdmsProcesses         (FK -> Adm)
--   9. Adms                  (main table)
-- ============================================================

USE [DL_SERVER]
GO

-- ### Enter the ADM Id to delete ###
DECLARE @AdmId INT = 1;
-- ##################################

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000; -- retry logic below handles transient blocking

-- ============================================================
-- Check if ADM exists
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Adms] WHERE Id = @AdmId)
BEGIN
    PRINT '========================================';
    PRINT 'ERROR: ADM Id = ' + CAST(@AdmId AS NVARCHAR(10)) + ' not found.';
    PRINT '========================================';
    RETURN;
END

-- ============================================================
-- Print ADM info before deletion
-- ============================================================
PRINT '========================================';
PRINT 'Target ADM Info';
PRINT '========================================';

SELECT Id, Name, LocalIp, MacAddress, CpuId, Status, CreatedAt
FROM [dbo].[Adms]
WHERE Id = @AdmId;

PRINT '';
PRINT 'Related AdmsProcess list:';
SELECT ap.Id AS AdmsProcessId, p.Name AS ProcessName
FROM [dbo].[AdmsProcesses] ap
JOIN [dbo].[Processes] p ON ap.ProcessId = p.Id
WHERE ap.AdmsId = @AdmId;

-- ============================================================
-- Build target id sets (prevents heavy OR scans)
-- ============================================================
IF OBJECT_ID('tempdb..#TargetAdmsProcess') IS NOT NULL DROP TABLE #TargetAdmsProcess;
IF OBJECT_ID('tempdb..#TargetAdmsProcessType') IS NOT NULL DROP TABLE #TargetAdmsProcessType;
IF OBJECT_ID('tempdb..#TargetImageFile') IS NOT NULL DROP TABLE #TargetImageFile;
IF OBJECT_ID('tempdb..#TargetTrainingImageResult') IS NOT NULL DROP TABLE #TargetTrainingImageResult;

CREATE TABLE #TargetAdmsProcess (Id INT PRIMARY KEY);
CREATE TABLE #TargetAdmsProcessType (Id INT PRIMARY KEY);
CREATE TABLE #TargetImageFile (Id INT PRIMARY KEY);
CREATE TABLE #TargetTrainingImageResult (Id INT PRIMARY KEY);

INSERT INTO #TargetAdmsProcess (Id)
SELECT ap.Id
FROM [dbo].[AdmsProcesses] ap
WHERE ap.AdmsId = @AdmId;

INSERT INTO #TargetAdmsProcessType (Id)
SELECT apt.Id
FROM [dbo].[AdmsProcessTypes] apt
JOIN #TargetAdmsProcess tap ON tap.Id = apt.AdmsProcessId;

INSERT INTO #TargetImageFile (Id)
SELECT img.Id
FROM [dbo].[ImageFiles] img
JOIN #TargetAdmsProcess tap ON tap.Id = img.AdmsProcessId;

INSERT INTO #TargetTrainingImageResult (Id)
SELECT tir.Id
FROM [dbo].[TrainingImageResults] tir
JOIN #TargetAdmsProcess tap ON tap.Id = tir.AdmsProcessId
UNION
SELECT tir.Id
FROM [dbo].[TrainingImageResults] tir
JOIN #TargetImageFile tif ON tif.Id = tir.ImageFileId;

-- ============================================================
-- Count related data
-- ============================================================
PRINT '';
PRINT 'Related table row counts:';

DECLARE @cnt INT;

SELECT @cnt = COUNT(*)
FROM [dbo].[ConfusionMatrixImages] cmi
JOIN #TargetImageFile tif ON tif.Id = cmi.ImageFileId;
PRINT '  - ConfusionMatrixImages:  ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM #TargetTrainingImageResult;
PRINT '  - TrainingImageResults:   ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM [dbo].[ModelRecords] mr
    JOIN #TargetAdmsProcessType tapt ON tapt.Id = mr.AdmsProcessTypeId;
PRINT '  - ModelRecords:           ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM #TargetAdmsProcessType;
PRINT '  - AdmsProcessTypes:       ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM [dbo].[RecipeFiles] rf
    JOIN #TargetAdmsProcess tap ON tap.Id = rf.AdmsProcessId;
PRINT '  - RecipeFiles:            ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM [dbo].[TrainingAdmsProcess] tap
    JOIN #TargetAdmsProcess t ON t.Id = tap.AdmsProcessId;
PRINT '  - TrainingAdmsProcesses:  ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM #TargetImageFile;
PRINT '  - ImageFiles:             ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

SELECT @cnt = COUNT(*) FROM #TargetAdmsProcess;
PRINT '  - AdmsProcesses:          ' + CAST(@cnt AS NVARCHAR(10)) + ' rows';

PRINT '';

-- ============================================================
-- Delete within transaction
-- ============================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. ConfusionMatrixImages (child of ImageFile)
    DELETE cmi
    FROM [dbo].[ConfusionMatrixImages] cmi
    JOIN #TargetImageFile tif ON tif.Id = cmi.ImageFileId;
    PRINT '[1/9] ConfusionMatrixImages deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 2. TrainingImageResults (linked via ImageFile or AdmsProcess)
    DELETE tir
    FROM [dbo].[TrainingImageResults] tir
    JOIN #TargetTrainingImageResult ttir ON ttir.Id = tir.Id;
    PRINT '[2/9] TrainingImageResults deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 3. ModelRecords (AdmsProcessType -> AdmsProcess -> Adm)
    DELETE mr
    FROM [dbo].[ModelRecords] mr
    JOIN #TargetAdmsProcessType tapt ON tapt.Id = mr.AdmsProcessTypeId;
    PRINT '[3/9] ModelRecords deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 4. AdmsProcessTypes
    DELETE apt
    FROM [dbo].[AdmsProcessTypes] apt
    JOIN #TargetAdmsProcessType tapt ON tapt.Id = apt.Id;
    PRINT '[4/9] AdmsProcessTypes deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 5. RecipeFiles
    DELETE rf
    FROM [dbo].[RecipeFiles] rf
    JOIN #TargetAdmsProcess tap ON tap.Id = rf.AdmsProcessId;
    PRINT '[5/9] RecipeFiles deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 6. TrainingAdmsProcesses (retry on lock timeout)
    DECLARE @DeletedTrainingAdmsRows INT = 0;
    DECLARE @RetryCount INT = 0;
    DECLARE @MaxRetries INT = 12; -- about 3 minutes total (12 * 15s)

    WHILE (1 = 1)
    BEGIN
        BEGIN TRY
            DELETE tap
            FROM [dbo].[TrainingAdmsProcess] tap
            JOIN #TargetAdmsProcess t ON t.Id = tap.AdmsProcessId;

            SET @DeletedTrainingAdmsRows = @@ROWCOUNT;
            BREAK;
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() = 1222 AND @RetryCount < @MaxRetries
            BEGIN
                SET @RetryCount = @RetryCount + 1;
                PRINT 'Lock timeout on TrainingAdmsProcess delete. Retry '
                      + CAST(@RetryCount AS NVARCHAR(10)) + '/' + CAST(@MaxRetries AS NVARCHAR(10))
                      + ' after 2 seconds...';
                WAITFOR DELAY '00:00:02';
            END
            ELSE
            BEGIN
                THROW;
            END
        END CATCH
    END

    PRINT '[6/9] TrainingAdmsProcesses deleted (' + CAST(@DeletedTrainingAdmsRows AS NVARCHAR(10)) + ' rows)';

    -- 7. ImageFiles
    DELETE img
    FROM [dbo].[ImageFiles] img
    JOIN #TargetImageFile tif ON tif.Id = img.Id;
    PRINT '[7/9] ImageFiles deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 8. AdmsProcesses
    DELETE ap
    FROM [dbo].[AdmsProcesses] ap
    JOIN #TargetAdmsProcess tap ON tap.Id = ap.Id;
    PRINT '[8/9] AdmsProcesses deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    -- 9. Adm (main table)
    DELETE FROM [dbo].[Adms] WHERE Id = @AdmId;
    PRINT '[9/9] Adms deleted (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows)';

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '========================================';
    PRINT 'ADM Id = ' + CAST(@AdmId AS NVARCHAR(10)) + ' successfully deleted!';
    PRINT '========================================';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '========================================';
    PRINT 'ERROR! Transaction has been rolled back.';
    PRINT 'Error message: ' + ERROR_MESSAGE();
    PRINT 'Error number:  ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT 'Error line:    ' + CAST(ERROR_LINE() AS NVARCHAR(10));
    IF ERROR_NUMBER() = 1222
    BEGIN
        PRINT 'Hint: another session is holding a lock. Stop the running training/sync job and retry.';
    END
    PRINT '========================================';
END CATCH
GO
