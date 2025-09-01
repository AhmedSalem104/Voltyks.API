using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentKeyExpiresAtLastPaymentKeyInPaymentOrderEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastPaymentKey",
                table: "PaymentOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentKeyExpiresAt",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ChargingRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ChargingRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPaymentKey",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PaymentKeyExpiresAt",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ChargingRequests");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ChargingRequests");
        }
    }
}
