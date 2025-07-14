using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingImageResultTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingImageResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: false),
                    ImageFileId = table.Column<int>(type: "int", nullable: false),
                    TrueLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PredictedLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingImageResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingImageResults_ImageFiles_ImageFileId",
                        column: x => x.ImageFileId,
                        principalTable: "ImageFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingImageResults_TrainingRecords_TrainingRecordId",
                        column: x => x.TrainingRecordId,
                        principalTable: "TrainingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingImageResults_ImageFileId",
                table: "TrainingImageResults",
                column: "ImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingImageResults_TrainingRecordId",
                table: "TrainingImageResults",
                column: "TrainingRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingImageResults");
        }
    }
}
