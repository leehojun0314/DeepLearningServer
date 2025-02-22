using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmsProcessTypeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCategorized",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "IsTrainned",
                table: "AdmsProcesses");

            migrationBuilder.DropColumn(
                name: "LastSyncDate",
                table: "AdmsProcesses");

            migrationBuilder.CreateTable(
                name: "AdmsProcessType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmsProcessId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsTrainned = table.Column<bool>(type: "bit", nullable: false),
                    IsCategorized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmsProcessType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmsProcessType_AdmsProcesses_AdmsProcessId",
                        column: x => x.AdmsProcessId,
                        principalTable: "AdmsProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmsProcessTypes_AdmsProcessId",
                table: "AdmsProcessType",
                column: "AdmsProcessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmsProcessType");

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

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncDate",
                table: "AdmsProcesses",
                type: "datetime",
                nullable: true);
        }
    }
}
