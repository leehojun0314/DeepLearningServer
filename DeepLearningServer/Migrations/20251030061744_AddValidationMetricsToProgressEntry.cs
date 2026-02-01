using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationMetricsToProgressEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ValidationAccuracy",
                table: "ProgressEntries",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "ValidationError",
                table: "ProgressEntries",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationAccuracy",
                table: "ProgressEntries");

            migrationBuilder.DropColumn(
                name: "ValidationError",
                table: "ProgressEntries");
        }
    }
}
