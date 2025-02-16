using System;
using System.Collections.Generic;
using DeepLearningServer.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeepLearningServer.Models;

public partial class DlServerContext : DbContext
{

    private readonly IConfiguration _configuration;

    public DlServerContext(DbContextOptions<DlServerContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public virtual DbSet<Adms> Adms { get; set; }

    public virtual DbSet<AdmsProcess> AdmsProcesses { get; set; }

    public virtual DbSet<Label> Labels { get; set; }

    public virtual DbSet<LogRecord> LogRecords { get; set; }

    public virtual DbSet<Process> Processes { get; set; }

    public virtual DbSet<ProgressEntry> ProgressEntries { get; set; }

    public virtual DbSet<RecipeFile> RecipeFiles { get; set; }

    public virtual DbSet<TrainingRecord> TrainingRecords { get; set; }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Server = www.dtizen.com, 1433; Uid = dtizen; Pwd = #lee353535; database = DL_SERVER; TrustServerCertificate=True;");
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetSection("DatabaseSettings:ConnectionStringMS").Value;
            optionsBuilder.UseSqlServer(connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(DlServerContext).Assembly.GetName().Name));
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Adms>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<AdmsProcess>(entity =>
        {
            entity.HasIndex(e => e.AdmsId, "IX_AdmsProcesses_AdmsId");

            entity.HasIndex(e => e.ProcessId, "IX_AdmsProcesses_ProcessId");

            entity.HasOne(d => d.Adms).WithMany(p => p.AdmsProcesses).HasForeignKey(d => d.AdmsId);

            entity.HasOne(d => d.Process).WithMany(p => p.AdmsProcesses).HasForeignKey(d => d.ProcessId);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasIndex(e => e.TrainingRecordId, "IX_Labels_TrainingRecordId");

            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.TrainingRecord).WithMany(p => p.Labels).HasForeignKey(d => d.TrainingRecordId);
        });

        modelBuilder.Entity<Process>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LastSyncDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<ProgressEntry>(entity =>
        {
            entity.HasIndex(e => e.TrainingRecordId, "IX_ProgressEntries_TrainingRecordId");

            entity.HasOne(d => d.TrainingRecord).WithMany(p => p.ProgressEntries).HasForeignKey(d => d.TrainingRecordId);
        });

        modelBuilder.Entity<RecipeFile>(entity =>
        {
            entity.HasIndex(e => e.AdmsId, "IX_RecipeFiles_AdmsId");

            entity.HasIndex(e => e.ProcessId, "IX_RecipeFiles_ProcessId");

            entity.Property(e => e.FileName).HasMaxLength(100);
            entity.Property(e => e.LastModified).HasColumnType("datetime");

            entity.HasOne(d => d.Adms).WithMany(p => p.RecipeFiles).HasForeignKey(d => d.AdmsId);

            entity.HasOne(d => d.Process).WithMany(p => p.RecipeFiles).HasForeignKey(d => d.ProcessId);
        });

        modelBuilder.Entity<TrainingRecord>(entity =>
        {
            entity.Property(e => e.SettingId).HasColumnName("SettingID");
            entity.Property(e => e.Status).HasMaxLength(50).HasConversion(new ValueConverter<TrainingStatus, string>(v => v.ToString(), v => (TrainingStatus)Enum.Parse(typeof(TrainingStatus), v)));
            
        });
        modelBuilder.Entity<LogRecord>(entity =>
        {
            var loglevelConverter = new ValueConverter<LogLevel, string>(v => v.ToString(), v => (LogLevel)Enum.Parse(typeof(LogLevel), v));
            entity.Property(e=>e.Level).HasConversion(loglevelConverter);
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
