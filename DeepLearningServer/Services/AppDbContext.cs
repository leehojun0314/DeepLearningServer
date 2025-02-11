using DeepLearningServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeepLearningServer.Services
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TrainingRecordModel> TrainingRecords { get; set; }
        public DbSet<LogRecordModel> LogRecords { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<ProgressHistory> ProgressHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 엔터티 구성 설정 (예: 테이블 이름, 관계 설정 등)
            modelBuilder.Entity<TrainingRecordModel>().ToTable("TrainingRecords");
            modelBuilder.Entity<LogRecordModel>().ToTable("LogRecords");

            // 일대다 관계 설정
            modelBuilder.Entity<TrainingRecordModel>()
                .HasMany(tr => tr.ProgressHistory)
                .WithOne(pe => pe.TrainingRecord)
                .HasForeignKey(pe => pe.TrainingRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TrainingRecordModel>()
                .HasMany(tr => tr.Labels)
                .WithOne(l => l.TrainingRecord)
                .HasForeignKey(l => l.TrainingRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // 추가적인 Fluent API 설정 가능
        }
    }
}
