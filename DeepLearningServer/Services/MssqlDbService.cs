using DeepLearningServer.Models;
using DeepLearningServer.Settings;
using Microsoft.EntityFrameworkCore;
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
            optionsBuilder.UseSqlServer(_dbSettings.ConnectionStringMS);
            return optionsBuilder.Options;
        }

        public async Task InsertLogAsync(string message, LogLevel logLevel)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var logRecord = new LogRecord
            {
                Message = message,
                Level = logLevel,
                CreatedAt = DateTime.UtcNow
            };
            context.LogRecords.Add(logRecord);
            await context.SaveChangesAsync();
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
        public async Task InsertTrainingAsync(TrainingRecord trainingRecord)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            context.TrainingRecords.Add(trainingRecord);
            await context.SaveChangesAsync();
        }

        public async Task PartialUpdateTrainingAsync(int id, Dictionary<string, object> updates)
        {
            using var context = new DlServerContext(GetDbContextOptions(), _configuration);
            var trainingRecord = await context.TrainingRecords.FindAsync(id);
            if (trainingRecord == null)
                throw new NullReferenceException("TrainingRecord not found");

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
