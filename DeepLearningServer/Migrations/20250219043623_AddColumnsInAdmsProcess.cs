using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsInAdmsProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncDate",
                table: "Processes");

            migrationBuilder.AddColumn<bool>(
                name: "IsCategorized",
                table: "AdmsProcesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrainned",
                table: "AdmsProcesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "L",
                table: "AdmsProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncDate",
                table: "AdmsProcesses",
                type: "datetime",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCategorized",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "IsTrainned",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "L",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "LastSyncDate",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "M",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "S",
                table: "AdmsProcesses");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncDate",
                table: "Processes",
                type: "datetime",
                nullable: true);
        }
    }
}
