using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtendMobileAppConfigPlatformVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MobileAppEnabled",
                table: "MobileAppConfigs",
                newName: "IosEnabled");

            migrationBuilder.AddColumn<bool>(
                name: "AndroidEnabled",
                table: "MobileAppConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "AndroidMinVersion",
                table: "MobileAppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IosMinVersion",
                table: "MobileAppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 23, 20, 18, 16, 596, DateTimeKind.Utc).AddTicks(7659));

            migrationBuilder.UpdateData(
                table: "MobileAppConfigs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AndroidEnabled", "AndroidMinVersion", "IosMinVersion" },
                values: new object[] { true, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AndroidEnabled",
                table: "MobileAppConfigs");

            migrationBuilder.DropColumn(
                name: "AndroidMinVersion",
                table: "MobileAppConfigs");

            migrationBuilder.DropColumn(
                name: "IosMinVersion",
                table: "MobileAppConfigs");

            migrationBuilder.RenameColumn(
                name: "IosEnabled",
                table: "MobileAppConfigs",
                newName: "MobileAppEnabled");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 22, 20, 16, 8, 458, DateTimeKind.Utc).AddTicks(1559));
        }
    }
}
