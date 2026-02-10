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

            // ============================================================
            // 1. SQL TRIGGER: Block direct Admin role assignment
            //    Only allows insert when session context flag is set by our API code.
            // ============================================================
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_BlockDirectAdminRoleInsert
ON AspNetUserRoles
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AdminRoleId NVARCHAR(450);
    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

    IF EXISTS (SELECT 1 FROM inserted WHERE RoleId = @AdminRoleId)
    BEGIN
        DECLARE @Allowed SQL_VARIANT;
        SET @Allowed = SESSION_CONTEXT(N'AllowAdminRoleInsert');

        IF @Allowed IS NULL OR CAST(@Allowed AS INT) <> 1
        BEGIN
            RAISERROR('Direct admin role assignment is blocked. Use the API endpoint POST /api/admin/users/create-admin.', 16, 1);
            RETURN;
        END
    END

    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT UserId, RoleId FROM inserted;
END
");

            // ============================================================
            // 2. RESTRICTED APP USER (Azure SQL contained database user)
            //    No db_owner, no ALTER — only data read/write.
            // ============================================================
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'voltyksapp')
BEGIN
    CREATE USER voltyksapp WITH PASSWORD = 'V0ltyks@ppSecure2025#!';
    ALTER ROLE db_datareader ADD MEMBER voltyksapp;
    ALTER ROLE db_datawriter ADD MEMBER voltyksapp;
END
");

            // ============================================================
            // 3. DENY ROLE: Block human/SSMS access to Identity tables
            //    Add any human DB account to this role to block them.
            // ============================================================
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'HumanAccessRole')
    CREATE ROLE HumanAccessRole;

DENY SELECT, INSERT, UPDATE, DELETE ON AspNetUsers TO HumanAccessRole;
DENY SELECT, INSERT, UPDATE, DELETE ON AspNetRoles TO HumanAccessRole;
DENY SELECT, INSERT, UPDATE, DELETE ON AspNetUserRoles TO HumanAccessRole;
DENY SELECT, INSERT, UPDATE, DELETE ON AspNetUserClaims TO HumanAccessRole;
DENY SELECT, INSERT, UPDATE, DELETE ON AspNetRoleClaims TO HumanAccessRole;
DENY SELECT, INSERT, UPDATE, DELETE ON AspNetUserLogins TO HumanAccessRole;
DENY SELECT, INSERT, UPDATE, DELETE ON AspNetUserTokens TO HumanAccessRole;
");
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
