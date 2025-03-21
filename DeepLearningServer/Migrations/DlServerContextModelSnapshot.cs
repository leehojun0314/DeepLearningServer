﻿// <auto-generated />
using System;
using DeepLearningServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DeepLearningServer.Migrations
{
    [DbContext(typeof(DlServerContext))]
    partial class DlServerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DeepLearningServer.Models.Adm", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CpuId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime");

                    b.Property<string>("LocalIp")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("MacAddress")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime");

                    b.HasKey("Id");

                    b.ToTable("Adms");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcess", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsId")
                        .HasColumnType("int");

                    b.Property<int>("ProcessId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsId" }, "IX_AdmsProcesses_AdmsId");

                    b.HasIndex(new[] { "ProcessId" }, "IX_AdmsProcesses_ProcessId");

                    b.ToTable("AdmsProcesses");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcessType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsProcessId")
                        .HasColumnType("int");

                    b.Property<bool>("IsCategorized")
                        .HasColumnType("bit");

                    b.Property<bool>("IsTrainned")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastSyncDate")
                        .HasColumnType("datetime");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsProcessId" }, "IX_AdmsProcessTypes_AdmsProcessId");

                    b.ToTable("AdmsProcessTypes");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ImageFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsProcessId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CapturedTime")
                        .HasColumnType("datetime");

                    b.Property<string>("Directory")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Size")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsProcessId" }, "IX_ImageFiles_AdmsProcessId");

                    b.ToTable("ImageFiles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Label", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float?>("Accuracy")
                        .HasColumnType("real");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("TrainingRecordId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "TrainingRecordId" }, "IX_Labels_TrainingRecordId");

                    b.ToTable("Labels");
                });

            modelBuilder.Entity("DeepLearningServer.Models.LogRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime");

                    b.Property<string>("Level")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("LogRecords");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ModelRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsProcessTypeId")
                        .HasColumnType("int");

                    b.Property<string>("ClientPath")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime");

                    b.Property<string>("ModelName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("ServerPath")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int?>("TrainingRecordId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsProcessTypeId" }, "IX_ModelRecords_AdmsProcessTypeId");

                    b.HasIndex(new[] { "TrainingRecordId" }, "IX_ModelRecords_TrainingRecordId");

                    b.ToTable("ModelRecords");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Permission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Permissions");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Process", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime");

                    b.HasKey("Id");

                    b.ToTable("Processes");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ProgressEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float?>("Accuracy")
                        .HasColumnType("real");

                    b.Property<double>("BestIteration")
                        .HasColumnType("float");

                    b.Property<bool>("IsTraining")
                        .HasColumnType("bit");

                    b.Property<double>("Progress")
                        .HasColumnType("float");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<int>("TrainingRecordId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "TrainingRecordId" }, "IX_ProgressEntries_TrainingRecordId");

                    b.ToTable("ProgressEntries");
                });

            modelBuilder.Entity("DeepLearningServer.Models.PwdResetRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsUsed")
                        .HasColumnType("bit");

                    b.Property<DateTime>("RequestedAt")
                        .HasColumnType("datetime");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "UserId" }, "IX_PwdResetRequests_UserId");

                    b.ToTable("PwdResetRequests");
                });

            modelBuilder.Entity("DeepLearningServer.Models.RecipeFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsProcessId")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("FileType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("datetime");

                    b.Property<string>("SyncStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsProcessId" }, "IX_RecipeFiles_AdmsProcessId");

                    b.ToTable("RecipeFiles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.RolePermission", b =>
                {
                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.Property<int>("PermissionId")
                        .HasColumnType("int");

                    b.HasKey("RoleId", "PermissionId");

                    b.HasIndex("PermissionId");

                    b.ToTable("RolePermissions");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingAdmsProcess", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsProcessId")
                        .HasColumnType("int");

                    b.Property<int>("TrainingRecordId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsProcessId" }, "IX_TrainingAdmsProcesses_AdmsProcessId");

                    b.HasIndex(new[] { "TrainingRecordId" }, "IX_TrainingAdmsProcesses_TrainingRecordId");

                    b.ToTable("TrainingAdmsProcess");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<float?>("Accuracy")
                        .HasColumnType("real");

                    b.Property<int>("BatchSize")
                        .HasColumnType("int");

                    b.Property<int?>("BestIteration")
                        .HasColumnType("int");

                    b.Property<int>("ClassifierCapacity")
                        .HasColumnType("int");

                    b.Property<bool>("ComputeHeatMap")
                        .HasColumnType("bit");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("EnableHistogramEqualization")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("HasPretrainedModel")
                        .HasColumnType("bit");

                    b.Property<bool>("HorizontalFlip")
                        .HasColumnType("bit");

                    b.Property<float>("HueOffset")
                        .HasColumnType("real");

                    b.Property<int>("ImageCacheSize")
                        .HasColumnType("int");

                    b.Property<int>("ImageChannels")
                        .HasColumnType("int");

                    b.Property<int>("ImageHeight")
                        .HasColumnType("int");

                    b.Property<int>("ImageSize")
                        .HasColumnType("int");

                    b.Property<int>("ImageWidth")
                        .HasColumnType("int");

                    b.Property<double?>("Loss")
                        .HasColumnType("float");

                    b.Property<float>("MaxBrightnessOffset")
                        .HasColumnType("real");

                    b.Property<float>("MaxContrastGain")
                        .HasColumnType("real");

                    b.Property<float>("MaxGamma")
                        .HasColumnType("real");

                    b.Property<float>("MaxGaussianDeviation")
                        .HasColumnType("real");

                    b.Property<float>("MaxHorizontalShear")
                        .HasColumnType("real");

                    b.Property<float>("MaxHorizontalShift")
                        .HasColumnType("real");

                    b.Property<float>("MaxRotation")
                        .HasColumnType("real");

                    b.Property<float>("MaxSaltPepperNoise")
                        .HasColumnType("real");

                    b.Property<float>("MaxSaturationGain")
                        .HasColumnType("real");

                    b.Property<float>("MaxScale")
                        .HasColumnType("real");

                    b.Property<float>("MaxSpeckleDeviation")
                        .HasColumnType("real");

                    b.Property<float>("MaxVerticalShear")
                        .HasColumnType("real");

                    b.Property<float>("MaxVerticalShift")
                        .HasColumnType("real");

                    b.Property<float>("MinContrastGain")
                        .HasColumnType("real");

                    b.Property<float>("MinGamma")
                        .HasColumnType("real");

                    b.Property<float>("MinGaussianDeviation")
                        .HasColumnType("real");

                    b.Property<float>("MinSaltPepperNoise")
                        .HasColumnType("real");

                    b.Property<float>("MinSaturationGain")
                        .HasColumnType("real");

                    b.Property<float>("MinScale")
                        .HasColumnType("real");

                    b.Property<float>("MinSpeckleDeviation")
                        .HasColumnType("real");

                    b.Property<float?>("Progress")
                        .HasColumnType("real");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool>("UsePretrainedModel")
                        .HasColumnType("bit");

                    b.Property<bool>("VerticalFlip")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("TrainingRecords");
                });

            modelBuilder.Entity("DeepLearningServer.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Email")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DeepLearningServer.Models.UserRole", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcess", b =>
                {
                    b.HasOne("DeepLearningServer.Models.Adm", "Adms")
                        .WithMany("AdmsProcesses")
                        .HasForeignKey("AdmsId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.Process", "Process")
                        .WithMany("AdmsProcesses")
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Adms");

                    b.Navigation("Process");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcessType", b =>
                {
                    b.HasOne("DeepLearningServer.Models.AdmsProcess", "AdmsProcess")
                        .WithMany("AdmsProcessTypes")
                        .HasForeignKey("AdmsProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdmsProcess");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ImageFile", b =>
                {
                    b.HasOne("DeepLearningServer.Models.AdmsProcess", "AdmsProcess")
                        .WithMany("ImageFiles")
                        .HasForeignKey("AdmsProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdmsProcess");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Label", b =>
                {
                    b.HasOne("DeepLearningServer.Models.TrainingRecord", "TrainingRecord")
                        .WithMany("Labels")
                        .HasForeignKey("TrainingRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TrainingRecord");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ModelRecord", b =>
                {
                    b.HasOne("DeepLearningServer.Models.AdmsProcessType", "AdmsProcessType")
                        .WithMany("ModelRecords")
                        .HasForeignKey("AdmsProcessTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.TrainingRecord", "TrainingRecord")
                        .WithMany("ModelRecords")
                        .HasForeignKey("TrainingRecordId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("AdmsProcessType");

                    b.Navigation("TrainingRecord");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ProgressEntry", b =>
                {
                    b.HasOne("DeepLearningServer.Models.TrainingRecord", "TrainingRecord")
                        .WithMany("ProgressEntries")
                        .HasForeignKey("TrainingRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TrainingRecord");
                });

            modelBuilder.Entity("DeepLearningServer.Models.PwdResetRequest", b =>
                {
                    b.HasOne("DeepLearningServer.Models.User", "User")
                        .WithMany("PwdResetRequests")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DeepLearningServer.Models.RecipeFile", b =>
                {
                    b.HasOne("DeepLearningServer.Models.AdmsProcess", "AdmsProcess")
                        .WithMany("RecipeFiles")
                        .HasForeignKey("AdmsProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdmsProcess");
                });

            modelBuilder.Entity("DeepLearningServer.Models.RolePermission", b =>
                {
                    b.HasOne("DeepLearningServer.Models.Permission", "Permission")
                        .WithMany("RolePermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.Role", "Role")
                        .WithMany("RolePermissions")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Permission");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingAdmsProcess", b =>
                {
                    b.HasOne("DeepLearningServer.Models.AdmsProcess", "AdmsProcess")
                        .WithMany("TrainingAdmsProcesses")
                        .HasForeignKey("AdmsProcessId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.TrainingRecord", "TrainingRecord")
                        .WithMany("TrainingAdmsProcesses")
                        .HasForeignKey("TrainingRecordId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("AdmsProcess");

                    b.Navigation("TrainingRecord");
                });

            modelBuilder.Entity("DeepLearningServer.Models.UserRole", b =>
                {
                    b.HasOne("DeepLearningServer.Models.Role", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Adm", b =>
                {
                    b.Navigation("AdmsProcesses");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcess", b =>
                {
                    b.Navigation("AdmsProcessTypes");

                    b.Navigation("ImageFiles");

                    b.Navigation("RecipeFiles");

                    b.Navigation("TrainingAdmsProcesses");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcessType", b =>
                {
                    b.Navigation("ModelRecords");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Permission", b =>
                {
                    b.Navigation("RolePermissions");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Process", b =>
                {
                    b.Navigation("AdmsProcesses");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Role", b =>
                {
                    b.Navigation("RolePermissions");

                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingRecord", b =>
                {
                    b.Navigation("Labels");

                    b.Navigation("ModelRecords");

                    b.Navigation("ProgressEntries");

                    b.Navigation("TrainingAdmsProcesses");
                });

            modelBuilder.Entity("DeepLearningServer.Models.User", b =>
                {
                    b.Navigation("PwdResetRequests");

                    b.Navigation("UserRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
