using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTrainingImageStructure_FixCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                table: "ImageFiles");

            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_Name_Directory_AdmsProcessId",
                table: "ImageFiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TrainingImageResults",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "AdmsProcessId",
                table: "TrainingImageResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "TrainingImageResults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TrainingImageResults",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "AdmsProcessId",
                table: "ImageFiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ImageFiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingImageResults_AdmsProcessId",
                table: "TrainingImageResults",
                column: "AdmsProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingImageResults_TrainingRecord_Labels",
                table: "TrainingImageResults",
                columns: new[] { "TrainingRecordId", "TrueLabel", "PredictedLabel" });

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Name_Directory_AdmsProcessId",
                table: "ImageFiles",
                columns: new[] { "Name", "Directory", "AdmsProcessId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Name_Directory_Category",
                table: "ImageFiles",
                columns: new[] { "Name", "Directory", "Category" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                table: "ImageFiles",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingImageResults_AdmsProcesses_AdmsProcessId",
                table: "TrainingImageResults",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                table: "ImageFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingImageResults_AdmsProcesses_AdmsProcessId",
                table: "TrainingImageResults");

            migrationBuilder.DropIndex(
                name: "IX_TrainingImageResults_AdmsProcessId",
                table: "TrainingImageResults");

            migrationBuilder.DropIndex(
                name: "IX_TrainingImageResults_TrainingRecord_Labels",
                table: "TrainingImageResults");

            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_Name_Directory_AdmsProcessId",
                table: "ImageFiles");

            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_Name_Directory_Category",
                table: "ImageFiles");

            migrationBuilder.DropColumn(
                name: "AdmsProcessId",
                table: "TrainingImageResults");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TrainingImageResults");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrainingImageResults");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ImageFiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TrainingImageResults",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<int>(
                name: "AdmsProcessId",
                table: "ImageFiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Name_Directory_AdmsProcessId",
                table: "ImageFiles",
                columns: new[] { "Name", "Directory", "AdmsProcessId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                table: "ImageFiles",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
