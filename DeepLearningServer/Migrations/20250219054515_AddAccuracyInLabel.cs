using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAccuracyInLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Accuracy",
                table: "Labels",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accuracy",
                table: "Labels");
        }
    }
}
