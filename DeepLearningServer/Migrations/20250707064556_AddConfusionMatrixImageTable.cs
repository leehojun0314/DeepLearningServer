using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddConfusionMatrixImageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfusionMatrixImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfusionMatrixId = table.Column<int>(type: "int", nullable: false),
                    ImageFileId = table.Column<int>(type: "int", nullable: false),
                    ActualPredictedLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfusionMatrixImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfusionMatrixImages_ConfusionMatrices_ConfusionMatrixId",
                        column: x => x.ConfusionMatrixId,
                        principalTable: "ConfusionMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfusionMatrixImages_ImageFiles_ImageFileId",
                        column: x => x.ImageFileId,
                        principalTable: "ImageFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfusionMatrixImages_ConfusionMatrixId",
                table: "ConfusionMatrixImages",
                column: "ConfusionMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfusionMatrixImages_ImageFileId",
                table: "ConfusionMatrixImages",
                column: "ImageFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfusionMatrixImages");
        }
    }
}
