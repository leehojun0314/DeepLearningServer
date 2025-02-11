using DeepLearningServer.Models;
using DeepLearningServer.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DeepLearningServer.Services
{
    public class SqlDbService
    {
        private readonly SqlDbSettings _dbSettings;

        public SqlDbService(IOptions<SqlDbSettings> dbSettings)
        {
            _dbSettings = dbSettings.Value;
        }

        private DbContextOptions<AppDbContext> GetDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(_dbSettings.ConnectionStringMS);
            return optionsBuilder.Options;
        }

        public async Task InsertLogAsync(string message, LogLevel logLevel)
        {
            using var context = new AppDbContext(GetDbContextOptions());
            var logRecord = new LogRecordModel
            {
                Message = message,
                Level = logLevel,
                CreatedAt = DateTime.UtcNow
            };
            context.LogRecords.Add(logRecord);
            await context.SaveChangesAsync();
        }

        public async Task InsertTrainingAsync(TrainingRecordModel trainingRecord)
        {
            using var context = new AppDbContext(GetDbContextOptions());
            context.TrainingRecords.Add(trainingRecord);
            await context.SaveChangesAsync();
        }

        public async Task PartialUpdateTrainingAsync(int id, Dictionary<string, object> updates)
        {
            using var context = new AppDbContext(GetDbContextOptions());
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

        public async Task PushProgressEntryAsync(int recordId, ProgressHistory newEntry)
        {
            using var context = new AppDbContext(GetDbContextOptions());
            var trainingRecord = await context.TrainingRecords
                .Include(tr => tr.ProgressHistory)
                .FirstOrDefaultAsync(tr => tr.Id == recordId);

            if (trainingRecord == null)
                throw new NullReferenceException("TrainingRecord not found");

            trainingRecord.ProgressHistory.Add(newEntry);
            await context.SaveChangesAsync();
        }

        public async Task UpdateLabelsByIdAsync(int id, Dictionary<string, float> labels)
        {
            using var context = new AppDbContext(GetDbContextOptions());
            var trainingRecord = await context.TrainingRecords
                .Include(tr => tr.Labels)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (trainingRecord == null)
                throw new NullReferenceException("TrainingRecord not found");

            // 기존 레이블 삭제
            context.Labels.RemoveRange(trainingRecord.Labels);

            // 새로운 레이블 추가
            foreach (var kvp in labels)
            {
                var label = new Label
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    TrainingRecordId = id
                };
                context.Labels.Add(label);
            }

            await context.SaveChangesAsync();
        }

    }
}
