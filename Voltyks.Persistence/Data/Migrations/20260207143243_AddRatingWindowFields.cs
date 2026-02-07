using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingWindowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultRatingApplied",
                table: "Process",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatingWindowOpenedAt",
                table: "Process",
                type: "datetime2",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRatingApplied",
                table: "Process");

            migrationBuilder.DropColumn(
                name: "RatingWindowOpenedAt",
                table: "Process");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 27, 10, 32, 6, 556, DateTimeKind.Utc).AddTicks(815));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 27, 10, 32, 6, 563, DateTimeKind.Utc).AddTicks(3441));
        }
    }
}
