using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditConditionInRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingRequests_AspNetUsers_UserId",
                table: "ChargingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ChargingRequests_Chargers_ChargerId",
                table: "ChargingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ChargingRequests_RelatedRequestId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChargingRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingRequests_Status_RequestedAt",
                table: "ChargingRequests",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingRequests_AspNetUsers_UserId",
                table: "ChargingRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingRequests_Chargers_ChargerId",
                table: "ChargingRequests",
                column: "ChargerId",
                principalTable: "Chargers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ChargingRequests_RelatedRequestId",
                table: "Notifications",
                column: "RelatedRequestId",
                principalTable: "ChargingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChargingRequests_AspNetUsers_UserId",
                table: "ChargingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ChargingRequests_Chargers_ChargerId",
                table: "ChargingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ChargingRequests_RelatedRequestId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ChargingRequests_Status_RequestedAt",
                table: "ChargingRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChargingRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingRequests_AspNetUsers_UserId",
                table: "ChargingRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChargingRequests_Chargers_ChargerId",
                table: "ChargingRequests",
                column: "ChargerId",
                principalTable: "Chargers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ChargingRequests_RelatedRequestId",
                table: "Notifications",
                column: "RelatedRequestId",
                principalTable: "ChargingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
