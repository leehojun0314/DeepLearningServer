using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdmsProcessIdFromTrainingRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                table: "TrainingRecords");

            migrationBuilder.AlterColumn<int>(
                name: "AdmsProcessId",
                table: "TrainingRecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                table: "TrainingRecords",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                table: "TrainingRecords");

            migrationBuilder.AlterColumn<int>(
                name: "AdmsProcessId",
                table: "TrainingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_AdmsProcesses_AdmsProcessId",
                table: "TrainingRecords",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
