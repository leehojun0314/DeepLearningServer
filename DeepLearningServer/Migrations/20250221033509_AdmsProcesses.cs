using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AdmsProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_Adms_AdmId",
                table: "ImageFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_Processes_ProcessId",
                table: "ImageFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeFiles_Adms_AdmsId",
                table: "RecipeFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeFiles_Processes_ProcessId",
                table: "RecipeFiles");

            migrationBuilder.DropIndex(
                name: "IX_RecipeFiles_AdmsId",
                table: "RecipeFiles");

            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_AdmId",
                table: "ImageFiles");

            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_ProcessId",
                table: "ImageFiles");

            migrationBuilder.DropColumn(
                name: "AdmsId",
                table: "RecipeFiles");

            migrationBuilder.DropColumn(
                name: "AdmId",
                table: "ImageFiles");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "ImageFiles");

            migrationBuilder.RenameColumn(
                name: "ProcessId",
                table: "RecipeFiles",
                newName: "AdmsProcessId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeFiles_ProcessId",
                table: "RecipeFiles",
                newName: "IX_RecipeFiles_AdmsProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeFiles_AdmsProcesses_AdmsProcessId",
                table: "RecipeFiles",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeFiles_AdmsProcesses_AdmsProcessId",
                table: "RecipeFiles");

            migrationBuilder.RenameColumn(
                name: "AdmsProcessId",
                table: "RecipeFiles",
                newName: "ProcessId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeFiles_AdmsProcessId",
                table: "RecipeFiles",
                newName: "IX_RecipeFiles_ProcessId");

            migrationBuilder.AddColumn<int>(
                name: "AdmsId",
                table: "RecipeFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AdmId",
                table: "ImageFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessId",
                table: "ImageFiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeFiles_AdmsId",
                table: "RecipeFiles",
                column: "AdmsId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_AdmId",
                table: "ImageFiles",
                column: "AdmId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_ProcessId",
                table: "ImageFiles",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_Adms_AdmId",
                table: "ImageFiles",
                column: "AdmId",
                principalTable: "Adms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_Processes_ProcessId",
                table: "ImageFiles",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeFiles_Adms_AdmsId",
                table: "RecipeFiles",
                column: "AdmsId",
                principalTable: "Adms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeFiles_Processes_ProcessId",
                table: "RecipeFiles",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
