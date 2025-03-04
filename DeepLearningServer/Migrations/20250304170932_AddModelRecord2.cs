using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepLearningServer.Migrations
{
    /// <inheritdoc />
    public partial class AddModelRecord2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmsProcessType_AdmsProcesses_AdmsProcessId",
                table: "AdmsProcessType");

            migrationBuilder.DropForeignKey(
                name: "FK_ModelRecord_AdmsProcessType_AdmsProcessTypeId",
                table: "ModelRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelRecord",
                table: "ModelRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdmsProcessType",
                table: "AdmsProcessType");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "ModelPath",
                table: "TrainingRecords");

            migrationBuilder.RenameTable(
                name: "ModelRecord",
                newName: "ModelRecords");

            migrationBuilder.RenameTable(
                name: "AdmsProcessType",
                newName: "AdmsProcessTypes");

            migrationBuilder.AddColumn<int>(
                name: "TrainingRecordId",
                table: "ModelRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelRecords",
                table: "ModelRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdmsProcessTypes",
                table: "AdmsProcessTypes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ModelRecords_TrainingRecordId",
                table: "ModelRecords",
                column: "TrainingRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmsProcessTypes_AdmsProcesses_AdmsProcessId",
                table: "AdmsProcessTypes",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModelRecords_AdmsProcessTypes_AdmsProcessTypeId",
                table: "ModelRecords",
                column: "AdmsProcessTypeId",
                principalTable: "AdmsProcessTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModelRecords_TrainingRecords_TrainingRecordId",
                table: "ModelRecords",
                column: "TrainingRecordId",
                principalTable: "TrainingRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmsProcessTypes_AdmsProcesses_AdmsProcessId",
                table: "AdmsProcessTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_ModelRecords_AdmsProcessTypes_AdmsProcessTypeId",
                table: "ModelRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ModelRecords_TrainingRecords_TrainingRecordId",
                table: "ModelRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelRecords",
                table: "ModelRecords");

            migrationBuilder.DropIndex(
                name: "IX_ModelRecords_TrainingRecordId",
                table: "ModelRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdmsProcessTypes",
                table: "AdmsProcessTypes");

            migrationBuilder.DropColumn(
                name: "TrainingRecordId",
                table: "ModelRecords");

            migrationBuilder.RenameTable(
                name: "ModelRecords",
                newName: "ModelRecord");

            migrationBuilder.RenameTable(
                name: "AdmsProcessTypes",
                newName: "AdmsProcessType");

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelPath",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelRecord",
                table: "ModelRecord",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdmsProcessType",
                table: "AdmsProcessType",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmsProcessType_AdmsProcesses_AdmsProcessId",
                table: "AdmsProcessType",
                column: "AdmsProcessId",
                principalTable: "AdmsProcesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModelRecord_AdmsProcessType_AdmsProcessTypeId",
                table: "ModelRecord",
                column: "AdmsProcessTypeId",
                principalTable: "AdmsProcessType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
