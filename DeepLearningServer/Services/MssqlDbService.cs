using DeepLearningServer.Models;
using DeepLearningServer.Settings;
using DeepLearningServer.Enums;
using DeepLearningServer.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;

namespace DeepLearningServer.Services
{
    public class MssqlDbService
    {
        private readonly SqlDbSettings _dbSettings;
        private readonly IConfiguration _configuration;

        public MssqlDbService(IOptions<SqlDbSettings> dbSettings, IConfiguration configuration)
        {
            _dbSettings = dbSettings.Value;
            _configuration = configuration;
        }

        public DbContextOptions<DlServerContext> GetDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DlServerContext>();
            optionsBuilder.UseSqlServer(_dbSettings.DefaultConnection, sqlOptions =>
            {
                // Enable transient fault handling so long-lived servers recover after idle disconnects
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
            return optionsBuilder.Options;
        }

        /// <summary>
        /// 절대 경로를 상대 경로로 변환하고 '\' 를 '/'로 변경합니다.
        /// 예: "Z:\AI_CUT_MIDDLE\OK\ProcessName\BASE" -> "AI_CUT_MIDDLE/OK/ProcessName/BASE"
        /// </summary>
        public string ConvertToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return "";

            // 드라이브 경로 제거 (C:\, Z:\ 등)
            string relativePath = absolutePath;
            if (relativePath.Length >= 3 && relativePath[1] == ':' && relativePath[2] == '\\')
            {
                relativePath = relativePath.Substring(3); // "Z:\" 부분 제거
            }

            // '\' 를 '/'로 변경
            relativePath = relativePath.Replace('\\', '/');

            return relativePath;
        }

        public async Task InsertLogAsync(string message, LogLevel logLevel)
        {
            Console.WriteLine("Log: " + message);
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var logRecord = new LogRecord
            {
                Message = message,
                Level = logLevel,
                CreatedAt = DateTime.Now
            };
            context.LogRecords.Add(logRecord);
            await context.SaveChangesAsync();
        }
        public async Task<AdmsProcess> GetAdmsProcess(int admsId, int processId)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var admsProcess = await context.AdmsProcesses
                .FirstOrDefaultAsync(ap => ap.AdmsId == admsId && ap.ProcessId == processId);
            return admsProcess == null ? throw new NullReferenceException("AdmsProcess not found") : admsProcess;
        }
        public async Task<string> GetProcessNameById(int processId)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var process = await context.Processes.FindAsync(processId);
            if (process == null)
                throw new NullReferenceException("Process not found");
            return process.Name;
        }
        public async Task<Adm> GetAdmsById(int admsId)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            Adm adms = await context.Adms.FindAsync(admsId);
            if (adms == null)
                throw new NullReferenceException("Adms not found");
            return adms;
        }
        public async Task<Dictionary<string, int>> GetAdmsProcessInfo(int admsProcessId)
        {
            Console.WriteLine("Adms process id: " + admsProcessId);
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var admsProcess = await context.AdmsProcesses.FindAsync(admsProcessId);
            if (admsProcess == null)
                throw new NullReferenceException("AdmsProcess not found");
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            dictionary.Add("admsId", admsProcess.AdmsId);
            dictionary.Add("processId", admsProcess.ProcessId);
            return dictionary;
        }
        public async Task InsertModelRecordAsync(ModelRecord modelRecord)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            context.ModelRecords.Add(modelRecord);
            await context.SaveChangesAsync();
        }
        public async Task<List<Dictionary<string, object>>> GetAdmsProcessInfos(List<int> admsProcessIds)
        {
            Console.WriteLine("Adms process ids: " + string.Join(", ", admsProcessIds));

            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var admsProcesses = await context.AdmsProcesses
                .Where(ap => admsProcessIds.Contains(ap.Id)) // ✅ 여러 개의 ID 조회
                .ToListAsync();

            if (!admsProcesses.Any())
                throw new NullReferenceException("No AdmsProcess found");
            Console.WriteLine($"Found AdmsProcesses: {string.Join(",", admsProcesses.ToArray().Select(el => el.Id))}");
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            foreach (var admsProcess in admsProcesses)
            {
                var processName = await GetProcessNameById(admsProcess.ProcessId);
                result.Add(new Dictionary<string, object>
                {
                    { "admsId", admsProcess.AdmsId },
                    { "processId", admsProcess.ProcessId },
                    {"processName" ,  processName},
                    {"admsProcessId", admsProcess.Id }
                });
            }

            return result;
        }
        public async Task UpdateTrainingAsync(TrainingRecord trainingRecord)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            context.TrainingRecords.Update(trainingRecord);
            await context.SaveChangesAsync();
        }
        public async Task InsertTrainingAsync(TrainingRecord trainingRecord)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            context.TrainingRecords.Add(trainingRecord);
            await context.SaveChangesAsync();
        }
        public async Task AddRangeTrainingAdmsProcess(List<TrainingAdmsProcess> trainingAdmsProcess)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            context.TrainingAdmsProcess.AddRange(trainingAdmsProcess);
            await context.SaveChangesAsync();
        }
        public async Task<AdmsProcessType> GetAdmsProcessType(int admsProcessId)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var admsProcessType = await context.AdmsProcessTypes.FindAsync(admsProcessId);
            if (admsProcessType == null)
                throw new NullReferenceException("AdmsProcessType not found. AdmsProcessId: " + admsProcessId);
            return admsProcessType;
        }
        public async Task PartialUpdateTrainingAsync(int id, Dictionary<string, object> updates)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var trainingRecord = await context.TrainingRecords.FindAsync(id);
            if (trainingRecord == null)
            {
                Console.WriteLine($"TrainingRecord not found. Training record id: {id}");
                return;
            }
            //throw new NullReferenceException("TrainingRecord not found");

            foreach (var kvp in updates)
            {
                var property = typeof(TrainingRecord).GetProperty(kvp.Key);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(trainingRecord, kvp.Value);
                }
            }

            await context.SaveChangesAsync();
        }
        public async Task<bool> CheckIsTraining()
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var trainingRecords = await context.TrainingRecords.ToListAsync();
            foreach (var trainingRecord in trainingRecords)
            {
                if (trainingRecord.Status == Enums.TrainingStatus.Running)
                {
                    //throw new InvalidOperationException("Another training is in progress");
                    return true;
                }
            }
            ;
            return false;
        }
        public async Task<AdmsProcessType> GetOrCreateAdmsProcessType(int admsProcessId, string type)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            // 먼저 기존 AdmsProcessType 조회
            var admsProcessType = await context.AdmsProcessTypes
                .FirstOrDefaultAsync(p => p.AdmsProcessId == admsProcessId && p.Type == type);

            if (admsProcessType == null)
            {
                // AdmsProcessType이 없으면 생성
                admsProcessType = new AdmsProcessType
                {
                    AdmsProcessId = admsProcessId,
                    Type = type,
                    IsCategorized = false, // 기본값
                    IsTrainned = true, // 훈련이 완료된 상태로 생성
                    LastSyncDate = DateTime.Now
                };

                context.AdmsProcessTypes.Add(admsProcessType);
                await context.SaveChangesAsync();
                Console.WriteLine($"새로운 AdmsProcessType 생성: Type={type}, AdmsProcessId={admsProcessId}, Id={admsProcessType.Id}");
            }

            return admsProcessType;
        }
        public async Task PushProgressEntryAsync(int recordId, ProgressEntry newEntry)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var trainingRecord = await context.TrainingRecords
                .Include(tr => tr.ProgressEntries)  // 수정: Id 대신 ProgressHistory 포함
                .FirstOrDefaultAsync(tr => tr.Id == recordId);

            if (trainingRecord == null)
                throw new NullReferenceException("TrainingRecord not found");

            trainingRecord.ProgressEntries.Add(newEntry);
            await context.SaveChangesAsync();
        }

        public async Task UpdateProgressEntryAsync(ProgressEntry progressEntry)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            context.ProgressEntries.Update(progressEntry);
            await context.SaveChangesAsync();
        }

        public async Task UpdateLabelsByIdAsync(int id, Label[] labels)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var trainingRecord = await context.TrainingRecords
                .Include(tr => tr.Labels)  // 기존 레이블 포함해서 가져오기
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (trainingRecord == null)
                throw new NullReferenceException("TrainingRecord not found");

            // 기존 레이블 삭제
            context.Labels.RemoveRange(trainingRecord.Labels);

            // 새로운 레이블 추가
            foreach (var label in labels)
            {
                label.TrainingRecordId = id;
                context.Labels.Add(label);
            }

            await context.SaveChangesAsync();
        }

        // ⚠️ DEPRECATED: Use TrainingImageResult table instead
        [Obsolete("This method is deprecated. TrainingImageResult table is used instead.")]
        public async Task SaveConfusionMatrixAsync(int trainingRecordId, string trueLabel, string predictedLabel)
        {
            await InsertLogAsync("DEPRECATED: SaveConfusionMatrixAsync called. Use TrainingImageResult instead.", LogLevel.Warning);
            // Method intentionally left empty - no longer saves to ConfusionMatrix table
        }

        // ⚠️ DEPRECATED: Use GetTrainingConfusionMatrixAsync instead
        [Obsolete("This method is deprecated. Use GetTrainingConfusionMatrixAsync instead.")]
        public async Task<List<object>> GetConfusionMatricesAsync(int trainingRecordId)
        {
            await InsertLogAsync("DEPRECATED: GetConfusionMatricesAsync called. Use GetTrainingConfusionMatrixAsync instead.", LogLevel.Warning);
            return new List<object>();
        }

        // ⚠️ DEPRECATED: Use TrainingImageResult table instead
        [Obsolete("This method is deprecated. Use SaveTrainingImageResultAsync instead.")]
        public async Task SaveConfusionMatrixImageAsync(int confusionMatrixId, int imageFileId, string actualPredictedLabel, float? confidence = null)
        {
            await InsertLogAsync("DEPRECATED: SaveConfusionMatrixImageAsync called. Use SaveTrainingImageResultAsync instead.", LogLevel.Warning);
            // Method intentionally left empty - no longer saves to ConfusionMatrixImage table
        }

        // ⚠️ DEPRECATED: Use GetTrainingImagesByLabelsAsync instead
        [Obsolete("This method is deprecated. Use GetTrainingImagesByLabelsAsync instead.")]
        public async Task<List<ImageFile>> GetImagesByConfusionMatrixAsync(int trainingRecordId, string trueLabel, string predictedLabel)
        {
            await InsertLogAsync("DEPRECATED: GetImagesByConfusionMatrixAsync called. Use GetTrainingImagesByLabelsAsync instead.", LogLevel.Warning);
            return new List<ImageFile>();
        }

        // ⚠️ DEPRECATED: Use GetTrainingImagesByLabelsAsync instead
        [Obsolete("This method is deprecated. Use GetTrainingImagesByLabelsAsync instead.")]
        public async Task<List<ConfusionMatrixImage>> GetConfusionMatrixImagesAsync(int trainingRecordId, string trueLabel, string predictedLabel)
        {
            await InsertLogAsync("DEPRECATED: GetConfusionMatrixImagesAsync called. Use GetTrainingImagesByLabelsAsync instead.", LogLevel.Warning);
            return new List<ConfusionMatrixImage>();
        }



        // Add method to create ImageFile record
        public async Task<ImageFile> CreateImageFileAsync(string name, string directory, string size, string status, int? admsProcessId, DateTime capturedTime, string? category = null)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var imageFile = new ImageFile
            {
                Name = name,
                Directory = directory,
                Size = size,
                Status = status,
                AdmsProcessId = admsProcessId,
                Category = category,
                CapturedTime = capturedTime
            };

            context.ImageFiles.Add(imageFile);
            await context.SaveChangesAsync();

            return imageFile;
        }

        // Add method to batch save training images to database
        public async Task SaveTrainingImagesAsync(List<(string imagePath, string trueLabel, string status, string? category, int? admsProcessId)> trainingImageRecords, int trainingRecordId, ImageSize imageSize = ImageSize.Middle)
        {
            Console.WriteLine($"🔍 DEBUG: SaveTrainingImagesAsync called with {trainingImageRecords.Count} records, ImageSize: {imageSize}");
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            // 모든 레코드가 유효함 (NG 이미지는 AdmsProcessId가 null, OK 이미지는 값이 있음)
            var validRecords = trainingImageRecords.ToList();
            Console.WriteLine($"🔍 DEBUG: Processing {validRecords.Count} training image records (NG + OK images)");

            if (!validRecords.Any())
            {
                await InsertLogAsync("No training image records to save", LogLevel.Information);
                Console.WriteLine("⚠️ DEBUG: No records to save - returning early");
                return;
            }

            // 배치 처리를 위한 데이터 준비
            var imageInfos = validRecords.Select(record => new
            {
                record.imagePath,
                record.trueLabel,
                record.status,
                record.category,
                record.admsProcessId,
                fileName = Path.GetFileName(record.imagePath),
                directory = ConvertToRelativePath(Path.GetDirectoryName(record.imagePath) ?? ""),
                capturedTime = File.Exists(record.imagePath) ? File.GetCreationTime(record.imagePath) : DateTime.Now
            }).ToList();

            // 기존 ImageFile 레코드들을 배치로 조회 (중복 체크)
            // NG 이미지: fileName + directory + category로 식별
            // OK 이미지: fileName + directory + admsProcessId로 식별
            var existingImageFiles = new Dictionary<string, ImageFile>();

            foreach (var info in imageInfos)
            {
                ImageFile? existing = null;
                string key = "";

                if (info.admsProcessId.HasValue)
                {
                    // OK 이미지: fileName + directory + admsProcessId로 조회
                    key = $"{info.fileName}|{info.directory}|{info.admsProcessId}";
                    existing = await context.ImageFiles
                        .FirstOrDefaultAsync(img => img.Name == info.fileName &&
                                                   img.Directory == info.directory &&
                                                   img.AdmsProcessId == info.admsProcessId);
                }
                else
                {
                    // NG 이미지: fileName + directory + category로 조회
                    key = $"{info.fileName}|{info.directory}|{info.category}";
                    existing = await context.ImageFiles
                        .FirstOrDefaultAsync(img => img.Name == info.fileName &&
                                                   img.Directory == info.directory &&
                                                   img.Category == info.category);
                }

                if (existing != null)
                {
                    existingImageFiles[key] = existing;
                }
            }

            // 배치 저장 (EF Core의 SaveChanges가 자체 트랜잭션을 사용)
            try
            {
                int newRecordsCount = 0;
                int skippedRecordsCount = 0;
                int updatedRecordsCount = 0;

                foreach (var info in imageInfos)
                {
                    // 키 생성 (OK 이미지와 NG 이미지를 구분)
                    string key = info.admsProcessId.HasValue
                        ? $"{info.fileName}|{info.directory}|{info.admsProcessId}"
                        : $"{info.fileName}|{info.directory}|{info.category}";

                    if (existingImageFiles.ContainsKey(key))
                    {
                        // 기존 레코드가 있는 경우 - 필요시 Status 업데이트
                        var existingFile = existingImageFiles[key];
                        if (existingFile.Status != "Training")
                        {
                            existingFile.Status = "Training";
                            context.ImageFiles.Update(existingFile);
                            updatedRecordsCount++;

                            var identifier = info.admsProcessId.HasValue ? $"AdmsProcessId: {info.admsProcessId}" : $"Category: {info.category}";
                            Console.WriteLine($"Updated ImageFile status to 'Training': {info.fileName} for {identifier}");
                        }
                        else
                        {
                            skippedRecordsCount++;
                            var identifier = info.admsProcessId.HasValue ? $"AdmsProcessId: {info.admsProcessId}" : $"Category: {info.category}";
                            Console.WriteLine($"ImageFile already exists with correct status: {info.fileName} for {identifier}");
                        }
                    }
                    else
                    {
                        // 새 레코드 생성
                        var sizeString = imageSize switch
                        {
                            ImageSize.Middle => "Middle",
                            ImageSize.Large => "Large",
                            _ => "Middle" // 기본값
                        };

                        Console.WriteLine($"🔍 DEBUG: Creating ImageFile with Size: {sizeString} (ImageSize: {imageSize})");

                        var newImageFile = new ImageFile
                        {
                            Name = info.fileName,
                            Directory = info.directory,
                            Size = sizeString,
                            Status = info.status, // Base, New, 또는 Predicted
                            AdmsProcessId = info.admsProcessId, // OK 이미지: 값 있음, NG 이미지: null
                            Category = info.category, // NG 이미지: 값 있음, OK 이미지: null
                            CapturedTime = info.capturedTime
                        };

                        context.ImageFiles.Add(newImageFile);
                        newRecordsCount++;

                        var identifier = info.admsProcessId.HasValue ? $"AdmsProcessId: {info.admsProcessId}" : $"Category: {info.category}";
                        Console.WriteLine($"✅ DEBUG: Added new ImageFile record: {info.fileName} with Size: {sizeString} for {identifier}");
                        Console.WriteLine($"✅ DEBUG: Full directory path: {info.directory}");
                    }
                }

                // 모든 변경사항을 한 번에 저장 (내부적으로 트랜잭션 적용)
                await context.SaveChangesAsync();

                // 결과 로깅
                await InsertLogAsync($"Training images saved successfully - New: {newRecordsCount}, Updated: {updatedRecordsCount}, Skipped: {skippedRecordsCount}", LogLevel.Information);
                Console.WriteLine($"✅ DEBUG: SaveTrainingImagesAsync completed - ImageSize: {imageSize}, New: {newRecordsCount}, Updated: {updatedRecordsCount}, Skipped: {skippedRecordsCount}");
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("UNIQUE constraint") == true ||
                                                  dbEx.InnerException?.Message.Contains("duplicate key") == true)
            {
                System.Diagnostics.Debug.WriteLine($"Duplicate image file detected during batch save");
                await InsertLogAsync("Duplicate image file detected, transaction rolled back", LogLevel.Warning);

                // 중복이 발생한 경우 개별적으로 처리할 수도 있지만, 현재는 전체 롤백
                throw new InvalidOperationException("Duplicate image files detected. This might indicate concurrent training processes or data inconsistency.", dbEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveTrainingImagesAsync: {ex.Message}");
                await InsertLogAsync("Error saving training images (rolled back)", LogLevel.Error);
                throw;
            }
        }

        // ⚠️ DEPRECATED: Use TrainingImageResult table instead
        [Obsolete("This method is deprecated. Use GetTrainingImagesByLabelsAsync instead.")]
        public async Task<ConfusionMatrix?> FindConfusionMatrixAsync(int trainingRecordId, string trueLabel, string predictedLabel)
        {
            await InsertLogAsync("DEPRECATED: FindConfusionMatrixAsync called. Use GetTrainingImagesByLabelsAsync instead.", LogLevel.Warning);
            return null;
        }

        // Add method to find ImageFile by name, directory, and admsProcessId or category
        public async Task<ImageFile?> FindImageFileAsync(string fileName, string directory, int? admsProcessId = null, string? category = null)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            if (admsProcessId.HasValue)
            {
                // OK 이미지: fileName + directory + admsProcessId로 검색
                return await context.ImageFiles
                    .FirstOrDefaultAsync(img => img.Name == fileName &&
                                               img.Directory == directory &&
                                               img.AdmsProcessId == admsProcessId);
            }
            else if (!string.IsNullOrEmpty(category))
            {
                // NG 이미지: fileName + directory + category로 검색
                return await context.ImageFiles
                    .FirstOrDefaultAsync(img => img.Name == fileName &&
                                               img.Directory == directory &&
                                               img.Category == category);
            }
            else
            {
                // 일반 검색: fileName + directory만으로 검색
                return await context.ImageFiles
                    .FirstOrDefaultAsync(img => img.Name == fileName &&
                                               img.Directory == directory);
            }
        }

        // ===== 🎯 새로운 단순한 TrainingImageResult 메서드들 =====

        /// <summary>
        /// 단일 이미지의 예측 결과를 저장합니다
        /// </summary>
        public async Task SaveTrainingImageResultAsync(int trainingRecordId, int imageFileId, string trueLabel, string predictedLabel, float? confidence = null, string status = "Predicted", string? category = null, int? admsProcessId = null)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var result = new TrainingImageResult
            {
                TrainingRecordId = trainingRecordId,
                ImageFileId = imageFileId,
                TrueLabel = trueLabel.ToUpper(),
                PredictedLabel = predictedLabel.ToUpper(),
                Confidence = confidence,
                Status = status,
                Category = category,
                AdmsProcessId = admsProcessId,
                CreatedAt = DateTime.Now
            };

            context.TrainingImageResults.Add(result);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 훈련 기록의 혼동행렬을 동적으로 계산하여 반환합니다
        /// </summary>
        public async Task<List<object>> GetTrainingConfusionMatrixAsync(int trainingRecordId)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var confusionMatrix = await context.TrainingImageResults
                .Where(r => r.TrainingRecordId == trainingRecordId)
                .GroupBy(r => new { r.TrueLabel, r.PredictedLabel })
                .Select(g => new
                {
                    TrueLabel = g.Key.TrueLabel,
                    PredictedLabel = g.Key.PredictedLabel,
                    Count = g.Count(),
                    AvgConfidence = g.Average(x => x.Confidence ?? 0f)
                })
                .OrderBy(x => x.TrueLabel)
                .ThenBy(x => x.PredictedLabel)
                .ToListAsync();

            return confusionMatrix.Cast<object>().ToList();
        }

        /// <summary>
        /// 특정 TrueLabel -> PredictedLabel에 해당하는 이미지들을 반환합니다
        /// </summary>
        public async Task<List<object>> GetTrainingImagesByLabelsAsync(int trainingRecordId, string trueLabel, string predictedLabel)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var images = await context.TrainingImageResults
                .Include(r => r.ImageFile)
                .Where(r => r.TrainingRecordId == trainingRecordId &&
                           r.TrueLabel == trueLabel.ToUpper() &&
                           r.PredictedLabel == predictedLabel.ToUpper())
                .Select(r => new
                {
                    r.Id,
                    r.TrueLabel,
                    r.PredictedLabel,
                    r.Confidence,
                    r.CreatedAt,
                    ImageFile = new
                    {
                        r.ImageFile.Id,
                        r.ImageFile.Name,
                        r.ImageFile.Directory,
                        r.ImageFile.Size,
                        r.ImageFile.Status,
                        r.ImageFile.CapturedTime
                    }
                })
                .OrderByDescending(r => r.Confidence)
                .ToListAsync();

            return images.Cast<object>().ToList();
        }

        /// <summary>
        /// 디버깅을 위해 모든 ImageFile 레코드를 반환합니다
        /// </summary>
        public async Task<List<ImageFile>> GetAllImageFilesForTrainingAsync(int trainingRecordId)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            // Status가 "Training"인 ImageFile들 또는 최근에 생성된 ImageFile들을 반환
            var recentTime = DateTime.Now.AddHours(-2); // 최근 2시간 내에 생성된 것들

            return await context.ImageFiles
                .Where(img => img.Status == "Training" || img.CapturedTime >= recentTime)
                .OrderByDescending(img => img.CapturedTime)
                .ToListAsync();
        }

        // ===== 🚀 AdmsController 최적화를 위한 새로운 메서드들 =====

        /// <summary>
        /// 특정 이미지 크기의 NG 카테고리별 이미지 개수를 데이터베이스에서 가져옵니다
        /// </summary>
        public async Task<Dictionary<string, int>> GetNgCategoriesImageCountAsync(string imageSize)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var ngImages = await context.ImageFiles
                .Where(img => img.Size == imageSize &&
                             (img.Directory.Contains("/NG/BASE/") || img.Directory.Contains("/NG/NEW/")))
                .ToListAsync();

            var categoryCounts = new Dictionary<string, int>();

            foreach (var image in ngImages)
            {
                // Directory 경로에서 카테고리 추출
                // 예: "AI_CUT_MIDDLE/NG/BASE/CATEGORY1" -> "CATEGORY1"
                var pathParts = image.Directory.Split('/');
                var ngIndex = Array.FindIndex(pathParts, part => part == "NG");

                if (ngIndex >= 0 && ngIndex + 2 < pathParts.Length)
                {
                    var category = pathParts[ngIndex + 2].ToUpper();

                    if (categoryCounts.ContainsKey(category))
                        categoryCounts[category]++;
                    else
                        categoryCounts[category] = 1;
                }
            }

            return categoryCounts;
        }

        /// <summary>
        /// 특정 AdmsProcessId와 이미지 크기의 OK 이미지 개수를 데이터베이스에서 가져옵니다
        /// </summary>
        public async Task<Dictionary<string, int>> GetOkImageCountByProcessAsync(int admsProcessId, string imageSize)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var okImages = await context.ImageFiles
                .Where(img => img.AdmsProcessId == admsProcessId &&
                             img.Size == imageSize &&
                             img.Directory.Contains("/OK/"))
                .ToListAsync();

            var result = new Dictionary<string, int>
            {
                {"BASE", 0},
                {"NEW", 0}
            };

            foreach (var image in okImages)
            {
                if (image.Directory.Contains("/BASE"))
                    result["BASE"]++;
                else if (image.Directory.Contains("/NEW"))
                    result["NEW"]++;
            }

            return result;
        }

        /// <summary>
        /// 여러 AdmsProcessId들의 OK 이미지 개수를 일괄로 데이터베이스에서 가져옵니다
        /// </summary>
        public async Task<Dictionary<int, Dictionary<string, int>>> GetOkImageCountBulkAsync(List<int> admsProcessIds, string imageSize)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            var okImages = await context.ImageFiles
                .Where(img => img.AdmsProcessId.HasValue &&
                             admsProcessIds.Contains(img.AdmsProcessId.Value) &&
                             img.Size == imageSize &&
                             img.Directory.Contains("/OK/"))
                .ToListAsync();

            var result = new Dictionary<int, Dictionary<string, int>>();

            // 각 AdmsProcessId에 대해 초기화
            foreach (var processId in admsProcessIds)
            {
                result[processId] = new Dictionary<string, int>
                {
                    {"BASE", 0},
                    {"NEW", 0}
                };
            }

            // 이미지들을 분류하여 카운팅
            foreach (var image in okImages)
            {
                if (image.AdmsProcessId.HasValue && result.ContainsKey(image.AdmsProcessId.Value))
                {
                    if (image.Directory.Contains("/BASE"))
                        result[image.AdmsProcessId.Value]["BASE"]++;
                    else if (image.Directory.Contains("/NEW"))
                        result[image.AdmsProcessId.Value]["NEW"]++;
                }
            }

            return result;
        }

        /// <summary>
        /// 파일 시스템의 NG 이미지(\"NG/BASE\", \"NG/NEW\")를 스캔하여 데이터베이스의 ImageFiles 테이블과 동기화합니다.
        /// - NG 이미지는 AdmsProcessId를 null로 저장합니다
        /// - 카테고리는 경로의 NG/{BASE|NEW}/{category}에서 추출하여 대문자로 저장합니다
        /// - Size는 Middle/Large 문자열로 저장합니다
        /// - Status는 Base/New로 저장합니다
        /// </summary>
        public async Task<NgSyncResult> SyncNgImagesAsync(ImageSize imageSize, string imageRootPath)
        {
            var result = new NgSyncResult
            {
                ImageSize = imageSize == ImageSize.Middle ? "Middle" : "Large",
                ImageRoot = imageRootPath
            };

            if (string.IsNullOrWhiteSpace(imageRootPath) || !Directory.Exists(imageRootPath))
            {
                result.Errors.Add($"Image root path does not exist: {imageRootPath}");
                return result;
            }

            string ngBasePath = Path.Combine(imageRootPath, "NG", "BASE");
            string ngNewPath = Path.Combine(imageRootPath, "NG", "NEW");

            var files = new List<(string filePath, string status)>();
            if (Directory.Exists(ngBasePath))
            {
                foreach (var f in Directory.GetFiles(ngBasePath, "*.jpg", SearchOption.AllDirectories))
                {
                    files.Add((f, "Base"));
                }
            }
            if (Directory.Exists(ngNewPath))
            {
                foreach (var f in Directory.GetFiles(ngNewPath, "*.jpg", SearchOption.AllDirectories))
                {
                    files.Add((f, "New"));
                }
            }

            result.TotalFilesScanned = files.Count;

            if (files.Count == 0)
            {
                return result;
            }

            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            // 미리 존재하는 NG 이미지 키셋 로드 (해당 Size만)
            var existing = await context.ImageFiles
                .Where(img => img.Size == result.ImageSize && (img.Directory.Contains("/NG/BASE/") || img.Directory.Contains("/NG/NEW/")))
                .Select(img => new { img.Name, img.Directory, img.Category })
                .ToListAsync();

            var existingKeys = new HashSet<string>(existing.Select(e => $"{e.Name}|{e.Directory}|{e.Category}"));

            var toInsert = new List<ImageFile>();

            foreach (var (filePath, status) in files)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string absoluteDir = Path.GetDirectoryName(filePath) ?? string.Empty;
                    string relativeDir = ConvertToRelativePath(absoluteDir);

                    // 카테고리 추출: AI_CUT_{SIZE}/NG/{BASE|NEW}/{CATEGORY}/...
                    string category = ExtractNgCategoryFromRelativeDir(relativeDir);
                    if (string.IsNullOrEmpty(category))
                    {
                        // 카테고리를 추출하지 못하면 스킵
                        result.Skipped++;
                        continue;
                    }

                    string key = $"{fileName}|{relativeDir}|{category}";
                    if (existingKeys.Contains(key))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var newImage = new ImageFile
                    {
                        Name = fileName,
                        Directory = relativeDir,
                        Size = result.ImageSize,
                        Status = status,
                        AdmsProcessId = null,
                        Category = category,
                        CapturedTime = File.Exists(filePath) ? File.GetCreationTime(filePath) : DateTime.Now
                    };

                    toInsert.Add(newImage);

                    result.Inserted++;
                    if (!result.InsertedByCategory.ContainsKey(category))
                    {
                        result.InsertedByCategory[category] = 0;
                    }
                    result.InsertedByCategory[category]++;

                    // 중복 방지를 위해 키셋에 즉시 추가
                    existingKeys.Add(key);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex.Message);
                }
            }

            if (toInsert.Count > 0)
            {
                // Let EF handle its own transaction to be compatible with resilient execution strategy
                try
                {
                    await context.ImageFiles.AddRangeAsync(toInsert);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"DB save failed: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 파일 시스템의 특정 프로세스 OK 이미지("OK/{processName}/BASE", "OK/{processName}/NEW")를 스캔하여 ImageFiles 테이블과 동기화합니다.
        /// - OK 이미지는 지정된 AdmsProcessId로 저장합니다
        /// - Size는 Middle/Large 문자열로 저장합니다
        /// - Status는 Base/New로 저장합니다
        /// </summary>
        public async Task<OkSyncResult> SyncOkImagesByProcessAsync(int admsProcessId, ImageSize imageSize, string imageRootPath)
        {
            var result = new OkSyncResult
            {
                ImageSize = imageSize == ImageSize.Middle ? "Middle" : "Large",
                ImageRoot = imageRootPath,
                AdmsProcessId = admsProcessId
            };

            if (string.IsNullOrWhiteSpace(imageRootPath) || !Directory.Exists(imageRootPath))
            {
                result.Errors.Add($"Image root path does not exist: {imageRootPath}");
                return result;
            }

            using var context = new DlServerContext(GetDbContextOptions(), _configuration);

            // 프로세스 이름 조회
            var admsProcess = await context.AdmsProcesses
                .Include(ap => ap.Process)
                .FirstOrDefaultAsync(ap => ap.Id == admsProcessId);

            if (admsProcess == null)
            {
                result.Errors.Add($"AdmsProcess not found: {admsProcessId}");
                return result;
            }

            string processName = admsProcess.Process.Name;
            result.ProcessName = processName;

            string okBasePath = Path.Combine(imageRootPath, "OK", processName, "BASE");
            string okNewPath = Path.Combine(imageRootPath, "OK", processName, "NEW");

            var files = new List<(string filePath, string status)>();
            if (Directory.Exists(okBasePath))
            {
                foreach (var f in Directory.GetFiles(okBasePath, "*.jpg", SearchOption.AllDirectories))
                {
                    files.Add((f, "Base"));
                }
            }
            if (Directory.Exists(okNewPath))
            {
                foreach (var f in Directory.GetFiles(okNewPath, "*.jpg", SearchOption.AllDirectories))
                {
                    files.Add((f, "New"));
                }
            }

            result.TotalFilesScanned = files.Count;
            if (files.Count == 0)
            {
                return result;
            }

            // 기존 OK 이미지 키셋 로드 (해당 Size + AdmsProcessId만)
            var existing = await context.ImageFiles
                .Where(img => img.Size == result.ImageSize && img.AdmsProcessId == admsProcessId)
                .Select(img => new { img.Name, img.Directory })
                .ToListAsync();

            var existingKeys = new HashSet<string>(existing.Select(e => $"{e.Name}|{e.Directory}"));

            var toInsert = new List<ImageFile>();

            foreach (var (filePath, status) in files)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string absoluteDir = Path.GetDirectoryName(filePath) ?? string.Empty;
                    string relativeDir = ConvertToRelativePath(absoluteDir);

                    string key = $"{fileName}|{relativeDir}";
                    if (existingKeys.Contains(key))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var newImage = new ImageFile
                    {
                        Name = fileName,
                        Directory = relativeDir,
                        Size = result.ImageSize,
                        Status = status,
                        AdmsProcessId = admsProcessId,
                        Category = null,
                        CapturedTime = File.Exists(filePath) ? File.GetCreationTime(filePath) : DateTime.Now
                    };

                    toInsert.Add(newImage);

                    result.Inserted++;
                    if (status == "Base") result.InsertedBase++; else result.InsertedNew++;

                    // 중복 방지를 위해 키셋에 즉시 추가
                    existingKeys.Add(key);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex.Message);
                }
            }

            if (toInsert.Count > 0)
            {
                // Let EF handle its own transaction to be compatible with resilient execution strategy
                try
                {
                    await context.ImageFiles.AddRangeAsync(toInsert);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"DB save failed: {ex.Message}");
                }
            }

            return result;
        }

        private static string ExtractNgCategoryFromRelativeDir(string relativeDir)
        {
            if (string.IsNullOrEmpty(relativeDir)) return string.Empty;
            var parts = relativeDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int ngIndex = Array.FindIndex(parts, p => string.Equals(p, "NG", StringComparison.OrdinalIgnoreCase));
            if (ngIndex >= 0 && ngIndex + 2 < parts.Length)
            {
                // parts[ngIndex + 1] == BASE|NEW, parts[ngIndex + 2] == category
                return parts[ngIndex + 2].ToUpperInvariant();
            }
            return string.Empty;
        }
    }
}
