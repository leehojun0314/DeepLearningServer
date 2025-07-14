using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToImageFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_Name_Directory_AdmsProcessId",
                table: "ImageFiles",
                columns: new[] { "Name", "Directory", "AdmsProcessId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_Name_Directory_AdmsProcessId",
                table: "ImageFiles");
        }
    }
}
