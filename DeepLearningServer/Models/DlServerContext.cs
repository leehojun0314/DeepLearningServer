using System;
using System.Collections.Generic;
using DeepLearningServer.Enums;
using DeepLearningServer.Settings;
using Microsoft.EntityFrameworkCore;

namespace DeepLearningServer.Models;

public partial class DlServerContext : DbContext
{
    public DlServerContext()
    {
    }

    public DlServerContext(DbContextOptions<DlServerContext> options, IConfiguration configuration)
        : base(options)
    {
    }

    public virtual DbSet<Adm> Adms { get; set; }

    public virtual DbSet<AdmsProcess> AdmsProcesses { get; set; }

    public virtual DbSet<ImageFile> ImageFiles { get; set; }

    public virtual DbSet<Label> Labels { get; set; }

    public virtual DbSet<LogRecord> LogRecords { get; set; }

    public virtual DbSet<Process> Processes { get; set; }

    public virtual DbSet<ProgressEntry> ProgressEntries { get; set; }

    public virtual DbSet<RecipeFile> RecipeFiles { get; set; }

    public virtual DbSet<TrainingRecord> TrainingRecords { get; set; }
    public virtual DbSet<TrainingAdmsProcess> TrainingAdmsProcess { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Adm>(entity =>
        {
            entity.Property(e => e.CpuId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LocalIp).HasMaxLength(100);
            entity.Property(e => e.MacAddress).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired(false);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Process>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });
        modelBuilder.Entity<AdmsProcess>(entity =>
        {
            entity.HasIndex(e => e.AdmsId, "IX_AdmsProcesses_AdmsId");

            entity.HasIndex(e => e.ProcessId, "IX_AdmsProcesses_ProcessId");
            entity.HasOne(d => d.Adms).WithMany(p => p.AdmsProcesses).HasForeignKey(d => d.AdmsId).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Process).WithMany(p => p.AdmsProcesses).HasForeignKey(d => d.ProcessId).OnDelete(DeleteBehavior.Restrict);
            // ✅ TrainingAdmsProcess와의 다대다 관계 설정
            entity.HasMany(ap => ap.TrainingAdmsProcesses)
                .WithOne(tap => tap.AdmsProcess)
                .HasForeignKey(tap => tap.AdmsProcessId);

        });
        modelBuilder.Entity<AdmsProcessType>(entity =>
        {
            entity.HasIndex(e => e.AdmsProcessId, "IX_AdmsProcessTypes_AdmsProcessId");
            entity.Property(e => e.Type).HasMaxLength(10); // Small, Middle, Large
            entity.Property(e => e.LastSyncDate).HasColumnType("datetime");
            entity.Property(e => e.IsTrainned).HasColumnType("bit");
            entity.Property(e => e.IsCategorized).HasColumnType("bit");

            entity.HasOne(d => d.AdmsProcess)
                  .WithMany(p => p.AdmsProcessTypes)
                  .HasForeignKey(d => d.AdmsProcessId);
        });
        modelBuilder.Entity<ImageFile>(entity =>
        {
            //entity.HasIndex(e => e.AdmsId, "IX_ImageFiles_AdmsId");

            //entity.HasIndex(e => e.ProcessId, "IX_ImageFiles_ProcessId");
            entity.HasIndex(e => e.AdmsProcessId, "IX_ImageFiles_AdmsProcessId"); // ✅ 새로운 인덱스 추가

            entity.Property(e => e.CapturedTime).HasColumnType("datetime");
            entity.Property(e => e.Directory).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Size).HasMaxLength(10);
            entity.Property(e => e.Status).HasMaxLength(50);

            //entity.HasOne(d => d.Adms).WithMany(p => p.ImageFiles).HasForeignKey(d => d.AdmsId);

            //entity.HasOne(d => d.Process).WithMany(p => p.ImageFiles).HasForeignKey(d => d.ProcessId);
            entity.HasOne(d => d.AdmsProcess)
              .WithMany(p => p.ImageFiles)
              .HasForeignKey(d => d.AdmsProcessId)
              .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasIndex(e => e.TrainingRecordId, "IX_Labels_TrainingRecordId");

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Accuracy).IsRequired(false);
            entity.HasOne(d => d.TrainingRecord).WithMany(p => p.Labels).HasForeignKey(d => d.TrainingRecordId);
        });


        modelBuilder.Entity<ProgressEntry>(entity =>
        {
            entity.HasIndex(e => e.TrainingRecordId, "IX_ProgressEntries_TrainingRecordId");

            entity.HasOne(d => d.TrainingRecord).WithMany(p => p.ProgressEntries).HasForeignKey(d => d.TrainingRecordId);
        });

        modelBuilder.Entity<RecipeFile>(entity =>
        {
            //entity.HasIndex(e => e.AdmsId, "IX_RecipeFiles_AdmsId");

            //entity.HasIndex(e => e.ProcessId, "IX_RecipeFiles_ProcessId");
            entity.HasIndex(e => e.AdmsProcessId, "IX_RecipeFiles_AdmsProcessId");
            entity.Property(e => e.FileName).HasMaxLength(100);
            entity.Property(e => e.LastModified).HasColumnType("datetime");
            entity.Property(e => e.FileType).HasMaxLength(50);
            //entity.HasOne(d => d.Adms).WithMany(p => p.RecipeFiles).HasForeignKey(d => d.AdmsId);

            entity.HasOne(d => d.AdmsProcess).WithMany(p => p.RecipeFiles).HasForeignKey(d => d.AdmsProcessId);
            //entity.HasOne(d => d.AdmsProcess).WithMany(p => p.RecipeFiles)
        });

        modelBuilder.Entity<TrainingRecord>(entity =>
        {
            entity.HasMany(ap => ap.TrainingAdmsProcesses)
               .WithOne(tap => tap.TrainingRecord)
               .HasForeignKey(tap => tap.TrainingRecordId);
            entity.Property(e => e.Status).HasMaxLength(50).HasConversion<string>(v => v.ToString(), v => Enum.Parse<TrainingStatus>(v));
        });
        modelBuilder.Entity<LogRecord>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.Level).HasMaxLength(50).HasConversion<string>(v => v.ToString(), v => Enum.Parse<LogLevel>(v));
        });
        modelBuilder.Entity<TrainingAdmsProcess>(entity => {
            entity.HasIndex(e => e.AdmsProcessId, "IX_TrainingAdmsProcesses_AdmsProcessId");
            entity.HasIndex(e => e.TrainingRecordId, "IX_TrainingAdmsProcesses_TrainingRecordId");
            entity.HasOne(d => d.AdmsProcess).WithMany(p => p.TrainingAdmsProcesses).HasForeignKey(d => d.AdmsProcessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.TrainingRecord).WithMany(p => p.TrainingAdmsProcesses).HasForeignKey(d => d.TrainingRecordId).OnDelete(DeleteBehavior.Restrict);
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
