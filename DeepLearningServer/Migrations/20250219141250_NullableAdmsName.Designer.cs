﻿// <auto-generated />
using System;
using DeepLearningServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DeepLearningServer.Migrations
{
    [DbContext(typeof(DlServerContext))]
    [Migration("20250219141250_NullableAdmsName")]
    partial class NullableAdmsName
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

                    b.Property<bool>("IsCategorized")
                        .HasColumnType("bit");

                    b.Property<bool>("IsTrainned")
                        .HasColumnType("bit");

                    b.Property<int>("L")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastSyncDate")
                        .HasColumnType("datetime");

                    b.Property<int>("M")
                        .HasColumnType("int");

                    b.Property<int>("ProcessId")
                        .HasColumnType("int");

                    b.Property<int>("S")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsId" }, "IX_AdmsProcesses_AdmsId");

                    b.HasIndex(new[] { "ProcessId" }, "IX_AdmsProcesses_ProcessId");

                    b.ToTable("AdmsProcesses");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ImageFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsId")
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

                    b.Property<int>("ProcessId")
                        .HasColumnType("int");

                    b.Property<string>("Size")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsId" }, "IX_ImageFiles_AdmsId");

                    b.HasIndex(new[] { "ProcessId" }, "IX_ImageFiles_ProcessId");

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

            modelBuilder.Entity("DeepLearningServer.Models.RecipeFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdmsId")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("datetime");

                    b.Property<int>("ProcessId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "AdmsId" }, "IX_RecipeFiles_AdmsId");

                    b.HasIndex(new[] { "ProcessId" }, "IX_RecipeFiles_ProcessId");

                    b.ToTable("RecipeFiles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<double?>("Accuracy")
                        .HasColumnType("float");

                    b.Property<int>("AdmsProcessId")
                        .HasColumnType("int");

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

                    b.Property<string>("ModelName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ModelPath")
                        .HasColumnType("nvarchar(max)");

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

                    b.HasIndex(new[] { "AdmsProcessId" }, "IX_TrainingRecords_AdmsProcessId");

                    b.ToTable("TrainingRecords");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcess", b =>
                {
                    b.HasOne("DeepLearningServer.Models.Adm", "Adms")
                        .WithMany("AdmsProcesses")
                        .HasForeignKey("AdmsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.Process", "Process")
                        .WithMany("AdmsProcesses")
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Adms");

                    b.Navigation("Process");
                });

            modelBuilder.Entity("DeepLearningServer.Models.ImageFile", b =>
                {
                    b.HasOne("DeepLearningServer.Models.Adm", "Adms")
                        .WithMany("ImageFiles")
                        .HasForeignKey("AdmsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.Process", "Process")
                        .WithMany("ImageFiles")
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Adms");

                    b.Navigation("Process");
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

            modelBuilder.Entity("DeepLearningServer.Models.ProgressEntry", b =>
                {
                    b.HasOne("DeepLearningServer.Models.TrainingRecord", "TrainingRecord")
                        .WithMany("ProgressEntries")
                        .HasForeignKey("TrainingRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TrainingRecord");
                });

            modelBuilder.Entity("DeepLearningServer.Models.RecipeFile", b =>
                {
                    b.HasOne("DeepLearningServer.Models.Adm", "Adms")
                        .WithMany("RecipeFiles")
                        .HasForeignKey("AdmsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeepLearningServer.Models.Process", "Process")
                        .WithMany("RecipeFiles")
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Adms");

                    b.Navigation("Process");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingRecord", b =>
                {
                    b.HasOne("DeepLearningServer.Models.AdmsProcess", "AdmsProcess")
                        .WithMany("TrainingRecords")
                        .HasForeignKey("AdmsProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdmsProcess");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Adm", b =>
                {
                    b.Navigation("AdmsProcesses");

                    b.Navigation("ImageFiles");

                    b.Navigation("RecipeFiles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.AdmsProcess", b =>
                {
                    b.Navigation("TrainingRecords");
                });

            modelBuilder.Entity("DeepLearningServer.Models.Process", b =>
                {
                    b.Navigation("AdmsProcesses");

                    b.Navigation("ImageFiles");

                    b.Navigation("RecipeFiles");
                });

            modelBuilder.Entity("DeepLearningServer.Models.TrainingRecord", b =>
                {
                    b.Navigation("Labels");

                    b.Navigation("ProgressEntries");
                });
#pragma warning restore 612, 618
        }
    }
}
