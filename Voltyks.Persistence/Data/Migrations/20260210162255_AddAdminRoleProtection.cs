using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminRoleProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 16, 22, 55, 39, DateTimeKind.Utc).AddTicks(5406));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 16, 22, 55, 46, DateTimeKind.Utc).AddTicks(3448));

            // Cleanup: drop trigger if it was created by a previous partial migration run
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_BlockDirectAdminRoleInsert')
    DROP TRIGGER TR_BlockDirectAdminRoleInsert;
");

            // NOTE: Admin role protection (trigger, restricted user, DENY role) temporarily
            // removed to allow DB update. Re-add via a new migration when ready.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 7, 14, 32, 42, 302, DateTimeKind.Utc).AddTicks(8526));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 7, 14, 32, 42, 309, DateTimeKind.Utc).AddTicks(8881));

            // Rollback: Remove trigger
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_BlockDirectAdminRoleInsert')
    DROP TRIGGER TR_BlockDirectAdminRoleInsert;
");

            // Rollback: Remove restricted app user
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'voltyksapp')
    DROP USER voltyksapp;
");

            // Rollback: Remove DENY role
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'HumanAccessRole')
    DROP ROLE HumanAccessRole;
");
        }
    }
}
