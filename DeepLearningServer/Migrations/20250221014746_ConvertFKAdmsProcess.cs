using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class ConvertFKAdmsProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_Adms_AdmsId",
                table: "ImageFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_Processes_ProcessId",
                table: "ImageFiles");

            migrationBuilder.RenameColumn(
                name: "AdmsId",
                table: "ImageFiles",
                newName: "AdmsProcessId");

            migrationBuilder.RenameIndex(
                name: "IX_ImageFiles_AdmsId",
                table: "ImageFiles",
                newName: "IX_ImageFiles_AdmsProcessId");

            migrationBuilder.AlterColumn<int>(
                name: "ProcessId",
                table: "ImageFiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AdmId",
                table: "ImageFiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageFiles_AdmId",
                table: "ImageFiles",
                column: "AdmId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                table: "ImageFiles",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_AdmsProcesses_AdmsProcessId",
                table: "ImageFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_Adms_AdmId",
                table: "ImageFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ImageFiles_Processes_ProcessId",
                table: "ImageFiles");

            migrationBuilder.DropIndex(
                name: "IX_ImageFiles_AdmId",
                table: "ImageFiles");

            migrationBuilder.DropColumn(
                name: "AdmId",
                table: "ImageFiles");

            migrationBuilder.RenameColumn(
                name: "AdmsProcessId",
                table: "ImageFiles",
                newName: "AdmsId");

            migrationBuilder.RenameIndex(
                name: "IX_ImageFiles_AdmsProcessId",
                table: "ImageFiles",
                newName: "IX_ImageFiles_AdmsId");

            migrationBuilder.AlterColumn<int>(
                name: "ProcessId",
                table: "ImageFiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_Adms_AdmsId",
                table: "ImageFiles",
                column: "AdmsId",
                principalTable: "Adms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageFiles_Processes_ProcessId",
                table: "ImageFiles",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
