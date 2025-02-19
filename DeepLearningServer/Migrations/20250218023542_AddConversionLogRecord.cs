using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionLogRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "SettingID",
                table: "TrainingRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TrainingRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Progress",
                table: "TrainingRecords",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelPath",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Loss",
                table: "TrainingRecords",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BestIteration",
                table: "TrainingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Accuracy",
                table: "TrainingRecords",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdmsProcessId",
                table: "TrainingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LearningRateParameters",
                table: "ProgressEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncDate",
                table: "Processes",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "LogRecords",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "LogRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "LogRecords",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "CpuId",
                table: "Adms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocalIp",
                table: "Adms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Adms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

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
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    AdmsId = table.Column<int>(type: "int", nullable: false),
                    CapturedTime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageFiles_Adms_AdmsId",
                        column: x => x.AdmsId,
                        principalTable: "Adms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageFiles_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_AdmsProcessId",
                table: "TrainingRecords",
                column: "AdmsProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_AdmsId",
                table: "ImageFiles",
                column: "AdmsId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_ProcessId",
                table: "ImageFiles",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                table: "TrainingRecords",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                table: "TrainingRecords");

            migrationBuilder.DropTable(
                name: "ImageFiles");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRecords_AdmsProcessId",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "AdmsProcessId",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "LearningRateParameters",
                table: "ProgressEntries");

            migrationBuilder.DropColumn(
                name: "CpuId",
                table: "Adms");

            migrationBuilder.DropColumn(
                name: "LocalIp",
                table: "Adms");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Adms");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TrainingRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<float>(
                name: "Progress",
                table: "TrainingRecords",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "ModelPath",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ModelName",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "Loss",
                table: "TrainingRecords",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "BestIteration",
                table: "TrainingRecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "Accuracy",
                table: "TrainingRecords",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "ProcessId",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipeId",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SettingID",
                table: "TrainingRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncDate",
                table: "Processes",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "LogRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "LogRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "LogRecords",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");
        }
    }
}
