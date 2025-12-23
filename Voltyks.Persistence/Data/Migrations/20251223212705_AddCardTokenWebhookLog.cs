using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardTokenWebhookLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardTokenWebhookLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebhookId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CardToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryMonth = table.Column<int>(type: "int", nullable: true),
                    ExpiryYear = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHmacValid = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SavedCardId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTokenWebhookLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTokenWebhookLogs_UserSavedCards_SavedCardId",
                        column: x => x.SavedCardId,
                        principalTable: "UserSavedCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 23, 21, 27, 5, 11, DateTimeKind.Utc).AddTicks(6702));

            migrationBuilder.CreateIndex(
                name: "IX_CardTokenWebhookLogs_ReceivedAt",
                table: "CardTokenWebhookLogs",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CardTokenWebhookLogs_SavedCardId",
                table: "CardTokenWebhookLogs",
                column: "SavedCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTokenWebhookLogs_Status",
                table: "CardTokenWebhookLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CardTokenWebhookLogs_UserId",
                table: "CardTokenWebhookLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTokenWebhookLogs_WebhookId",
                table: "CardTokenWebhookLogs",
                column: "WebhookId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardTokenWebhookLogs");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 23, 20, 18, 16, 596, DateTimeKind.Utc).AddTicks(7659));
        }
    }
}
