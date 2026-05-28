using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2PerfIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The standalone FK index IX_Notifications_UserId is superseded by the new
            // composite (UserId, SentAt) — UserId is the leading column so UserId-only
            // lookups still seek efficiently.
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            // Skipping the auto-scaffolded UpdateData on AppSettings.UpdatedAt and
            // FeesConfigs.UpdatedAt — those are noise from HasData(..., UpdatedAt =
            // DateTime.UtcNow) at compile time and would overwrite real admin-edited
            // values in production. Indexes are the only intended change here.

            migrationBuilder.CreateIndex(
                name: "IX_StoreReservations_UserId_CreatedAt",
                table: "StoreReservations",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Processes_ChargerOwnerId_DateCreated",
                table: "Process",
                columns: new[] { "ChargerOwnerId", "DateCreated" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Processes_VehicleOwnerId_DateCreated",
                table: "Process",
                columns: new[] { "VehicleOwnerId", "DateCreated" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_SentAt",
                table: "Notifications",
                columns: new[] { "UserId", "SentAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ChargingRequests_RecipientUserId_Status_RequestedAt",
                table: "ChargingRequests",
                columns: new[] { "RecipientUserId", "Status", "RequestedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_UserId_IsActive_IsDeleted",
                table: "Chargers",
                columns: new[] { "UserId", "IsActive", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreReservations_UserId_CreatedAt",
                table: "StoreReservations");

            migrationBuilder.DropIndex(
                name: "IX_Processes_ChargerOwnerId_DateCreated",
                table: "Process");

            migrationBuilder.DropIndex(
                name: "IX_Processes_VehicleOwnerId_DateCreated",
                table: "Process");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_SentAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ChargingRequests_RecipientUserId_Status_RequestedAt",
                table: "ChargingRequests");

            migrationBuilder.DropIndex(
                name: "IX_Chargers_UserId_IsActive_IsDeleted",
                table: "Chargers");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
