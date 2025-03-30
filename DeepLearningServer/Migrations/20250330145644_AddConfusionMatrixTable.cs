using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddConfusionMatrixTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfusionMatrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: false),
                    TrueLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PredictedLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfusionMatrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfusionMatrices_TrainingRecords_TrainingRecordId",
                        column: x => x.TrainingRecordId,
                        principalTable: "TrainingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfusionMatrices_TrainingRecordId",
                table: "ConfusionMatrices",
                column: "TrainingRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfusionMatrices");
        }
    }
}
