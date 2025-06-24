using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressEntryTimingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "ProgressEntries",
                newName: "StartTime");

            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "ProgressEntries",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "ProgressEntries",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ProgressEntries");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "ProgressEntries");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "ProgressEntries",
                newName: "Timestamp");
        }
    }
}
