using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentActivitiesJson",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "AspNetUsers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Processes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargerRequestId = table.Column<int>(type: "int", nullable: false),
                    VehicleOwnerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargerOwnerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountCharged = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VehicleOwnerRating = table.Column<double>(type: "float", nullable: true),
                    ChargerOwnerRating = table.Column<double>(type: "float", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Processes_ChargingRequests_ChargerRequestId",
                        column: x => x.ChargerRequestId,
                        principalTable: "ChargingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RatingsHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    RaterUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RateeUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stars = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingsHistory", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 20, 14, 23, 53, 569, DateTimeKind.Utc).AddTicks(3036));

            migrationBuilder.CreateIndex(
                name: "IX_Processes_ChargerRequestId",
                table: "Processes",
                column: "ChargerRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingsHistory_ProcessId_RaterUserId",
                table: "RatingsHistory",
                columns: new[] { "ProcessId", "RaterUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Processes");

            migrationBuilder.DropTable(
                name: "RatingsHistory");

            migrationBuilder.DropColumn(
                name: "CurrentActivitiesJson",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 15, 0, 55, 41, 205, DateTimeKind.Utc).AddTicks(8894));
        }
    }
}
