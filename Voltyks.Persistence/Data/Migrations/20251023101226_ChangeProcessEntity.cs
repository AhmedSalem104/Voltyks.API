using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProcessEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processes_ChargingRequests_ChargerRequestId",
                table: "Processes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_Processes_ProcessId",
                table: "UserReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Processes",
                table: "Processes");

            migrationBuilder.RenameTable(
                name: "Processes",
                newName: "Process");

            migrationBuilder.RenameIndex(
                name: "IX_Processes_ChargerRequestId",
                table: "Process",
                newName: "IX_Process_ChargerRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Process",
                table: "Process",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 23, 10, 12, 25, 528, DateTimeKind.Utc).AddTicks(815));

            migrationBuilder.AddForeignKey(
                name: "FK_Process_ChargingRequests_ChargerRequestId",
                table: "Process",
                column: "ChargerRequestId",
                principalTable: "ChargingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_Process_ProcessId",
                table: "UserReports",
                column: "ProcessId",
                principalTable: "Process",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Process_ChargingRequests_ChargerRequestId",
                table: "Process");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_Process_ProcessId",
                table: "UserReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Process",
                table: "Process");

            migrationBuilder.RenameTable(
                name: "Process",
                newName: "Processes");

            migrationBuilder.RenameIndex(
                name: "IX_Process_ChargerRequestId",
                table: "Processes",
                newName: "IX_Processes_ChargerRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Processes",
                table: "Processes",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 23, 9, 2, 55, 256, DateTimeKind.Utc).AddTicks(440));

            migrationBuilder.AddForeignKey(
                name: "FK_Processes_ChargingRequests_ChargerRequestId",
                table: "Processes",
                column: "ChargerRequestId",
                principalTable: "ChargingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_Processes_ProcessId",
                table: "UserReports",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
