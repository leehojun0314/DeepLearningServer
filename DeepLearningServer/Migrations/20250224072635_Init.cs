using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocalIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CpuId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Processes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmsProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmsId = table.Column<int>(type: "int", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmsProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmsProcesses_Adms_AdmsId",
                        column: x => x.AdmsId,
                        principalTable: "Adms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmsProcesses_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmsProcessType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmsProcessId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsTrainned = table.Column<bool>(type: "bit", nullable: false),
                    IsCategorized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmsProcessType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmsProcessType_AdmsProcesses_AdmsProcessId",
                        column: x => x.AdmsProcessId,
                        principalTable: "AdmsProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Directory = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Size = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdmsProcessId = table.Column<int>(type: "int", nullable: false),
                    CapturedTime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                        column: x => x.AdmsProcessId,
                        principalTable: "AdmsProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmsProcessId = table.Column<int>(type: "int", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeFiles_AdmsProcesses_AdmsProcessId",
                        column: x => x.AdmsProcessId,
                        principalTable: "AdmsProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageSize = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdmsProcessId = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Accuracy = table.Column<double>(type: "float", nullable: true),
                    Loss = table.Column<double>(type: "float", nullable: true),
                    Progress = table.Column<float>(type: "real", nullable: true),
                    BestIteration = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxRotation = table.Column<float>(type: "real", nullable: false),
                    MaxVerticalShift = table.Column<float>(type: "real", nullable: false),
                    MaxHorizontalShift = table.Column<float>(type: "real", nullable: false),
                    MinScale = table.Column<float>(type: "real", nullable: false),
                    MaxScale = table.Column<float>(type: "real", nullable: false),
                    MaxVerticalShear = table.Column<float>(type: "real", nullable: false),
                    MaxHorizontalShear = table.Column<float>(type: "real", nullable: false),
                    VerticalFlip = table.Column<bool>(type: "bit", nullable: false),
                    HorizontalFlip = table.Column<bool>(type: "bit", nullable: false),
                    MaxBrightnessOffset = table.Column<float>(type: "real", nullable: false),
                    MaxContrastGain = table.Column<float>(type: "real", nullable: false),
                    MinContrastGain = table.Column<float>(type: "real", nullable: false),
                    MaxGamma = table.Column<float>(type: "real", nullable: false),
                    MinGamma = table.Column<float>(type: "real", nullable: false),
                    HueOffset = table.Column<float>(type: "real", nullable: false),
                    MaxSaturationGain = table.Column<float>(type: "real", nullable: false),
                    MinSaturationGain = table.Column<float>(type: "real", nullable: false),
                    MaxGaussianDeviation = table.Column<float>(type: "real", nullable: false),
                    MinGaussianDeviation = table.Column<float>(type: "real", nullable: false),
                    MaxSpeckleDeviation = table.Column<float>(type: "real", nullable: false),
                    MinSpeckleDeviation = table.Column<float>(type: "real", nullable: false),
                    MaxSaltPepperNoise = table.Column<float>(type: "real", nullable: false),
                    MinSaltPepperNoise = table.Column<float>(type: "real", nullable: false),
                    ClassifierCapacity = table.Column<int>(type: "int", nullable: false),
                    ImageCacheSize = table.Column<int>(type: "int", nullable: false),
                    ImageWidth = table.Column<int>(type: "int", nullable: false),
                    ImageHeight = table.Column<int>(type: "int", nullable: false),
                    ImageChannels = table.Column<int>(type: "int", nullable: false),
                    UsePretrainedModel = table.Column<bool>(type: "bit", nullable: false),
                    ComputeHeatMap = table.Column<bool>(type: "bit", nullable: false),
                    EnableHistogramEqualization = table.Column<bool>(type: "bit", nullable: false),
                    BatchSize = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                        column: x => x.AdmsProcessId,
                        principalTable: "AdmsProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Accuracy = table.Column<float>(type: "real", nullable: true),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labels_TrainingRecords_TrainingRecordId",
                        column: x => x.TrainingRecordId,
                        principalTable: "TrainingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsTraining = table.Column<bool>(type: "bit", nullable: false),
                    Progress = table.Column<double>(type: "float", nullable: false),
                    BestIteration = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressEntries_TrainingRecords_TrainingRecordId",
                        column: x => x.TrainingRecordId,
                        principalTable: "TrainingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAdmsProcess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: false),
                    AdmsProcessId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAdmsProcess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAdmsProcess_AdmsProcesses_AdmsProcessId",
                        column: x => x.AdmsProcessId,
                        principalTable: "AdmsProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingAdmsProcess_TrainingRecords_TrainingRecordId",
                        column: x => x.TrainingRecordId,
                        principalTable: "TrainingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmsProcesses_AdmsId",
                table: "AdmsProcesses",
                column: "AdmsId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmsProcesses_ProcessId",
                table: "AdmsProcesses",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmsProcessTypes_AdmsProcessId",
                table: "AdmsProcessType",
                column: "AdmsProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_AdmsProcessId",
                table: "ImageFiles",
                column: "AdmsProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_TrainingRecordId",
                table: "Labels",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressEntries_TrainingRecordId",
                table: "ProgressEntries",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeFiles_AdmsProcessId",
                table: "RecipeFiles",
                column: "AdmsProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAdmsProcesses_AdmsProcessId",
                table: "TrainingAdmsProcess",
                column: "AdmsProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAdmsProcesses_TrainingRecordId",
                table: "TrainingAdmsProcess",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_AdmsProcessId",
                table: "TrainingRecords",
                column: "AdmsProcessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmsProcessType");

            migrationBuilder.DropTable(
                name: "ImageFiles");

            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "LogRecords");

            migrationBuilder.DropTable(
                name: "ProgressEntries");

            migrationBuilder.DropTable(
                name: "RecipeFiles");

            migrationBuilder.DropTable(
                name: "TrainingAdmsProcess");

            migrationBuilder.DropTable(
                name: "TrainingRecords");

            migrationBuilder.DropTable(
                name: "AdmsProcesses");

            migrationBuilder.DropTable(
                name: "Adms");

            migrationBuilder.DropTable(
                name: "Processes");
        }
    }
}
