using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_UserGeneralComplaints_UserId",
                table: "UserGeneralComplaints");

            migrationBuilder.RenameIndex(
                name: "IX_Process_ChargerRequestId",
                table: "Process",
                newName: "IX_Processes_ChargerRequestId");

            migrationBuilder.AlterColumn<string>(
                name: "RateeUserId",
                table: "RatingsHistory",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleOwnerId",
                table: "Process",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ChargerOwnerId",
                table: "Process",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentOrders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PaymentOrders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "EGP",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "DeviceTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientUserId",
                table: "ChargingRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 30, 20, 59, 42, 417, DateTimeKind.Utc).AddTicks(1336));

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Plate",
                table: "Vehicles",
                column: "Plate");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId_IsDeleted",
                table: "Vehicles",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_UserGeneralComplaints_UserId_IsResolved",
                table: "UserGeneralComplaints",
                columns: new[] { "UserId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_RatingsHistory_ProcessId",
                table: "RatingsHistory",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingsHistory_RateeUserId",
                table: "RatingsHistory",
                column: "RateeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingsHistory_RaterUserId",
                table: "RatingsHistory",
                column: "RaterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Processes_ChargerOwnerId_Status",
                table: "Process",
                columns: new[] { "ChargerOwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Processes_VehicleOwnerId_Status",
                table: "Process",
                columns: new[] { "VehicleOwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_PaymobOrderId",
                table: "PaymentOrders",
                column: "PaymobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_UserId_Status",
                table: "PaymentOrders",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_Token",
                table: "DeviceTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingRequests_RecipientUserId",
                table: "ChargingRequests",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_IsDeleted_IsActive",
                table: "Chargers",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_IsDeleted_IsActive_ProtocolId",
                table: "Chargers",
                columns: new[] { "IsDeleted", "IsActive", "ProtocolId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email_IsBanned",
                table: "AspNetUsers",
                columns: new[] { "Email", "IsBanned" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsAvailable",
                table: "AspNetUsers",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsBanned",
                table: "AspNetUsers",
                column: "IsBanned");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber_IsBanned",
                table: "AspNetUsers",
                columns: new[] { "PhoneNumber", "IsBanned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Plate",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_UserId_IsDeleted",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_UserGeneralComplaints_UserId_IsResolved",
                table: "UserGeneralComplaints");

            migrationBuilder.DropIndex(
                name: "IX_RatingsHistory_ProcessId",
                table: "RatingsHistory");

            migrationBuilder.DropIndex(
                name: "IX_RatingsHistory_RateeUserId",
                table: "RatingsHistory");

            migrationBuilder.DropIndex(
                name: "IX_RatingsHistory_RaterUserId",
                table: "RatingsHistory");

            migrationBuilder.DropIndex(
                name: "IX_Processes_ChargerOwnerId_Status",
                table: "Process");

            migrationBuilder.DropIndex(
                name: "IX_Processes_VehicleOwnerId_Status",
                table: "Process");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_PaymobOrderId",
                table: "PaymentOrders");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_UserId_Status",
                table: "PaymentOrders");

            migrationBuilder.DropIndex(
                name: "IX_DeviceTokens_Token",
                table: "DeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_ChargingRequests_RecipientUserId",
                table: "ChargingRequests");

            migrationBuilder.DropIndex(
                name: "IX_Chargers_IsDeleted_IsActive",
                table: "Chargers");

            migrationBuilder.DropIndex(
                name: "IX_Chargers_IsDeleted_IsActive_ProtocolId",
                table: "Chargers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email_IsBanned",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsAvailable",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsBanned",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber_IsBanned",
                table: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_Processes_ChargerRequestId",
                table: "Process",
                newName: "IX_Process_ChargerRequestId");

            migrationBuilder.AlterColumn<string>(
                name: "RateeUserId",
                table: "RatingsHistory",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleOwnerId",
                table: "Process",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ChargerOwnerId",
                table: "Process",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "PaymentOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValue: "EGP");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "DeviceTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientUserId",
                table: "ChargingRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 11, 29, 22, 27, 11, 220, DateTimeKind.Utc).AddTicks(2988));

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGeneralComplaints_UserId",
                table: "UserGeneralComplaints",
                column: "UserId");
        }
    }
}
