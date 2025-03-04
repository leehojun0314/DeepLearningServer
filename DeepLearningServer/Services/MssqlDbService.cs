using DeepLearningServer.Models;
using DeepLearningServer.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        private DbContextOptions<DlServerContext> GetDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DlServerContext>();
            optionsBuilder.UseSqlServer(_dbSettings.DefaultConnection);
            return optionsBuilder.Options;
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
                throw new NullReferenceException("AdmsProcessType not found");
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
            };
            return false;
        }
        //public async Task<AdmsProcessType> GetOrCreateAdmsProcessType(int admsProcessId, string type)
        //{
           
        //        using var context = new DlServerContext(GetDbContextOptions(), _configuration); var admsProcessType = await _context.AdmsProcessTypes
        //            .FirstOrDefaultAsync(p => p.AdmsProcessId == admsProcessId && p.Type == type);

        //        if (admsProcessType == null)
        //        {
        //            admsProcessType = new AdmsProcessType
        //            {
        //                AdmsProcessId = admsProcessId,
        //                Type = type,
        //                IsCategorized = false, // 기본값
        //                IsTrainned = (type == "Small") ? false : false, // Small은 False, 나머지는 초기 False
        //                LastSyncDate = DateTime.UtcNow
        //            };

        //            context.AdmsP.Add(admsProcessType);
        //            await context.SaveChangesAsync();
        //            _logger.LogInformation("새로운 AdmsProcessType 생성: {Type}, AdmsProcessId: {AdmsProcessId}", type, admsProcessId);
        //        }
        //        return admsProcessType;
        //}
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

    }
}
