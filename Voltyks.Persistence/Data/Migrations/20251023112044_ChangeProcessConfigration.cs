using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProcessConfigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Process_ChargerRequestId",
                table: "Process");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 23, 11, 20, 43, 8, DateTimeKind.Utc).AddTicks(3926));

            migrationBuilder.CreateIndex(
                name: "IX_Process_ChargerRequestId",
                table: "Process",
                column: "ChargerRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Process_ChargerRequestId",
                table: "Process");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 23, 10, 12, 25, 528, DateTimeKind.Utc).AddTicks(815));

            migrationBuilder.CreateIndex(
                name: "IX_Process_ChargerRequestId",
                table: "Process",
                column: "ChargerRequestId",
                unique: true);
        }
    }
}
