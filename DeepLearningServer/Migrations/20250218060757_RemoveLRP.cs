using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLRP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LearningRateParameters",
                table: "ProgressEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LearningRateParameters",
                table: "ProgressEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
