﻿using System;
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
    public virtual DbSet<AdmsProcessType> AdmsProcessTypes { get; set; }
    public virtual DbSet<ModelRecord> ModelRecords { get; set; }
    public virtual DbSet<ImageFile> ImageFiles { get; set; }

    public virtual DbSet<Label> Labels { get; set; }

    public virtual DbSet<LogRecord> LogRecords { get; set; }

    public virtual DbSet<Process> Processes { get; set; }

    public virtual DbSet<ProgressEntry> ProgressEntries { get; set; }

    public virtual DbSet<RecipeFile> RecipeFiles { get; set; }

    public virtual DbSet<TrainingRecord> TrainingRecords { get; set; }
    public virtual DbSet<TrainingAdmsProcess> TrainingAdmsProcess { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<RolePermission> RolePermissions { get; set; }
    public virtual DbSet<PwdResetRequest> PwdResetRequests { get; set; }
    /// <summary>
    /// ⚠️ DEPRECATED: Use TrainingImageResults instead
    /// </summary>
    [Obsolete("This DbSet is deprecated. Use TrainingImageResults instead.")]
    public virtual DbSet<ConfusionMatrix> ConfusionMatrices { get; set; }

    /// <summary>
    /// ⚠️ DEPRECATED: Use TrainingImageResults instead
    /// </summary>
    [Obsolete("This DbSet is deprecated. Use TrainingImageResults instead.")]
    public virtual DbSet<ConfusionMatrixImage> ConfusionMatrixImages { get; set; }
    public virtual DbSet<TrainingImageResult> TrainingImageResults { get; set; }
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

            // ✅ 인덱스 추가 (고유 제약조건은 제거 - NG/OK 이미지 구분으로 인해 복잡함)
            entity.HasIndex(e => new { e.Name, e.Directory, e.AdmsProcessId }, "IX_ImageFiles_Name_Directory_AdmsProcessId");
            entity.HasIndex(e => new { e.Name, e.Directory, e.Category }, "IX_ImageFiles_Name_Directory_Category");

            entity.Property(e => e.CapturedTime).HasColumnType("datetime");
            entity.Property(e => e.Directory).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Size).HasMaxLength(10);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired(false);

            //entity.HasOne(d => d.Adms).WithMany(p => p.ImageFiles).HasForeignKey(d => d.AdmsId);

            //entity.HasOne(d => d.Process).WithMany(p => p.ImageFiles).HasForeignKey(d => d.ProcessId);
            entity.HasOne(d => d.AdmsProcess)
              .WithMany(p => p.ImageFiles)
              .HasForeignKey(d => d.AdmsProcessId)
              .OnDelete(DeleteBehavior.NoAction);
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
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
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
        modelBuilder.Entity<TrainingAdmsProcess>(entity =>
        {
            entity.HasIndex(e => e.AdmsProcessId, "IX_TrainingAdmsProcesses_AdmsProcessId");
            entity.HasIndex(e => e.TrainingRecordId, "IX_TrainingAdmsProcesses_TrainingRecordId");
            entity.HasOne(d => d.AdmsProcess).WithMany(p => p.TrainingAdmsProcesses).HasForeignKey(d => d.AdmsProcessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.TrainingRecord).WithMany(p => p.TrainingAdmsProcesses).HasForeignKey(d => d.TrainingRecordId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ModelRecord>(entity =>
        {
            entity.HasIndex(e => e.AdmsProcessTypeId, "IX_ModelRecords_AdmsProcessTypeId");
            entity.HasIndex(e => e.TrainingRecordId, "IX_ModelRecords_TrainingRecordId");

            entity.Property(e => e.ModelName).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ClientPath).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.ServerPath).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            // 🔹 AdmsProcessType 연결 (라지/미들 구분)
            entity.HasOne(d => d.AdmsProcessType)
                  .WithMany(p => p.ModelRecords)
                  .HasForeignKey(d => d.AdmsProcessTypeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 🔹 TrainingRecord 연결 (훈련 기록 참조)
            entity.HasOne(d => d.TrainingRecord)
                  .WithMany(p => p.ModelRecords)
                  .HasForeignKey(d => d.TrainingRecordId)
                  .OnDelete(DeleteBehavior.SetNull); // 훈련 기록 삭제 시 모델 유지
        });
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);
        modelBuilder.Entity<PwdResetRequest>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_PwdResetRequests_UserId");

            entity.Property(e => e.RequestedAt).HasColumnType("datetime");
            entity.Property(e => e.IsUsed).HasColumnType("bit");

            entity.HasOne(d => d.User)
                  .WithMany(p => p.PwdResetRequests)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // 유저 삭제 시 요청도 삭제
        });

        modelBuilder.Entity<ConfusionMatrix>(entity =>
        {
            entity.HasIndex(e => e.TrainingRecordId, "IX_ConfusionMatrices_TrainingRecordId");

            entity.Property(e => e.TrueLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PredictedLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.TrainingRecord)
                  .WithMany(p => p.ConfusionMatrices)
                  .HasForeignKey(d => d.TrainingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfusionMatrixImage>(entity =>
        {
            entity.HasIndex(e => e.ConfusionMatrixId, "IX_ConfusionMatrixImages_ConfusionMatrixId");
            entity.HasIndex(e => e.ImageFileId, "IX_ConfusionMatrixImages_ImageFileId");

            entity.Property(e => e.ActualPredictedLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Confidence).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.ConfusionMatrix)
                  .WithMany(p => p.ConfusionMatrixImages)
                  .HasForeignKey(d => d.ConfusionMatrixId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ImageFile)
                  .WithMany(p => p.ConfusionMatrixImages)
                  .HasForeignKey(d => d.ImageFileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ✅ TrainingImageResult Entity 설정 추가
        modelBuilder.Entity<TrainingImageResult>(entity =>
        {
            entity.HasIndex(e => e.TrainingRecordId, "IX_TrainingImageResults_TrainingRecordId");
            entity.HasIndex(e => e.ImageFileId, "IX_TrainingImageResults_ImageFileId");
            entity.HasIndex(e => e.AdmsProcessId, "IX_TrainingImageResults_AdmsProcessId");

            // 성능을 위한 복합 인덱스
            entity.HasIndex(e => new { e.TrainingRecordId, e.TrueLabel, e.PredictedLabel }, "IX_TrainingImageResults_TrainingRecord_Labels");

            entity.Property(e => e.TrueLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PredictedLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            // TrainingRecord와의 관계
            entity.HasOne(d => d.TrainingRecord)
                  .WithMany(p => p.TrainingImageResults)
                  .HasForeignKey(d => d.TrainingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);

            // ImageFile과의 관계
            entity.HasOne(d => d.ImageFile)
                  .WithMany(p => p.TrainingImageResults)
                  .HasForeignKey(d => d.ImageFileId)
                  .OnDelete(DeleteBehavior.Cascade);

            // AdmsProcess와의 관계 (nullable) - Cascade 충돌 방지를 위해 NoAction 사용
            entity.HasOne(d => d.AdmsProcess)
                  .WithMany()
                  .HasForeignKey(d => d.AdmsProcessId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
