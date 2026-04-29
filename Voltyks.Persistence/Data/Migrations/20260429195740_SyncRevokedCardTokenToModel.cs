using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncRevokedCardTokenToModel : Migration
    {
        // RevokedCardTokens table already exists in production from an earlier
        // migration; the entity was just missing from the EF model. This migration
        // exists only to bring the snapshot back in sync — schema is unchanged.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 29, 19, 57, 39, 968, DateTimeKind.Utc).AddTicks(9597));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 29, 19, 57, 39, 975, DateTimeKind.Utc).AddTicks(8606));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 27, 13, 18, 2, 343, DateTimeKind.Utc).AddTicks(469));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 27, 13, 18, 2, 350, DateTimeKind.Utc).AddTicks(7596));
        }
    }
}
