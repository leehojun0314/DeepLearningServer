using DeepLearningServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeepLearningServer.Services
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<TrainingRecord> TrainingRecords { get; set; }
        public DbSet<LogRecord> LogRecords { get; set; }
        public DbSet<Category> Labels { get; set; }
        public DbSet<ProgressEntry> ProgressEntries { get; set; }  // 수정: ProgressHistory -> ProgressEntry
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configuration.GetSection("DatabaseSettings:ConnectionStringMS").Value;
                optionsBuilder.UseSqlServer(connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name));
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 테이블 이름 설정
            modelBuilder.Entity<TrainingRecord>().ToTable("TrainingRecords");
            modelBuilder.Entity<LogRecord>().ToTable("LogRecords");

            // TrainingRecord와 ProgressEntry 간의 관계 설정 (1:N)
            modelBuilder.Entity<TrainingRecord>()
                .HasMany(tr => tr.ProgressHistory)
                .WithOne(pe => pe.TrainingRecord)
                .HasForeignKey(pe => pe.TrainingRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // TrainingRecord와 Label 간의 관계 설정 (1:N)
            modelBuilder.Entity<TrainingRecord>()
                .HasMany(tr => tr.Categories)
                .WithOne(l => l.TrainingRecord)
                .HasForeignKey(l => l.TrainingRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TrainingRecord>()
                  .Property(tr => tr.Status)
                  .HasConversion<string>(); // 🔹 Enum을 문자열로 변환하여 저장
        }
    }
}
