using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModuleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymobTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAmountCents = table.Column<long>(type: "bigint", nullable: false),
                    ProcessedAmountCents = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GatewayResponseCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GatewayResponseMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MerchantOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymobOrderId = table.Column<long>(type: "bigint", nullable: true),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOrders", x => x.Id);
                    table.UniqueConstraint("AK_PaymentOrders_MerchantOrderId", x => x.MerchantOrderId);
                });

            migrationBuilder.CreateTable(
                name: "WebhookLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MerchantOrderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymobOrderId = table.Column<long>(type: "bigint", nullable: true),
                    PaymobTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    IsHmacValid = table.Column<bool>(type: "bit", nullable: false),
                    HttpStatus = table.Column<int>(type: "int", nullable: true),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MerchantOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymobOrderId = table.Column<long>(type: "bigint", nullable: true),
                    PaymobTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    IntegrationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CapturedAmountCents = table.Column<long>(type: "bigint", nullable: false),
                    RefundedAmountCents = table.Column<long>(type: "bigint", nullable: false),
                    GatewayResponseCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GatewayResponseMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethodMasked = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardBrand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_PaymentOrders_MerchantOrderId",
                        column: x => x.MerchantOrderId,
                        principalTable: "PaymentOrders",
                        principalColumn: "MerchantOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_MerchantOrderId",
                table: "PaymentOrders",
                column: "MerchantOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MerchantOrderId",
                table: "PaymentTransactions",
                column: "MerchantOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymobTransactionId",
                table: "PaymentTransactions",
                column: "PaymobTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentActions");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "WebhookLogs");

            migrationBuilder.DropTable(
                name: "PaymentOrders");
        }
    }
}
