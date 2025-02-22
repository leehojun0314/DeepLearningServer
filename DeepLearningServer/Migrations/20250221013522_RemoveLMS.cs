using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLMS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "L",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "M",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "S",
                table: "AdmsProcesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "L",
                table: "AdmsProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "M",
                table: "AdmsProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "S",
                table: "AdmsProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
