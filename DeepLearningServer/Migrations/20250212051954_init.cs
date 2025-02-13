using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageSize = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettingID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Accuracy = table.Column<double>(type: "float", nullable: false),
                    Loss = table.Column<double>(type: "float", nullable: false),
                    Progress = table.Column<float>(type: "real", nullable: false),
                    BestIteration = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: false),
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
                    LearningRateParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Labels_TrainingRecordId",
                table: "Labels",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressEntries_TrainingRecordId",
                table: "ProgressEntries",
                column: "TrainingRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "LogRecords");

            migrationBuilder.DropTable(
                name: "ProgressEntries");

            migrationBuilder.DropTable(
                name: "TrainingRecords");
        }
    }
}
