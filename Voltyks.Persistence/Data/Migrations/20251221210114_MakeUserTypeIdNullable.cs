using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserTypeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_UserTypes_UserTypeId",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "UserTypeId",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 21, 21, 1, 13, 825, DateTimeKind.Utc).AddTicks(6647));

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_UserTypes_UserTypeId",
                table: "Notifications",
                column: "UserTypeId",
                principalTable: "UserTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_UserTypes_UserTypeId",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "UserTypeId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 21, 10, 42, 25, 739, DateTimeKind.Utc).AddTicks(1690));

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_UserTypes_UserTypeId",
                table: "Notifications",
                column: "UserTypeId",
                principalTable: "UserTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
