using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChargerAdaptersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargerAdapters",
                columns: table => new
                {
                    ChargerId = table.Column<int>(type: "int", nullable: false),
                    ProtocolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargerAdapters", x => new { x.ChargerId, x.ProtocolId });
                    table.ForeignKey(
                        name: "FK_ChargerAdapters_Chargers_ChargerId",
                        column: x => x.ChargerId,
                        principalTable: "Chargers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChargerAdapters_Protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 10, 20, 4, 57, 865, DateTimeKind.Utc).AddTicks(3894));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 5, 10, 20, 4, 57, 871, DateTimeKind.Utc).AddTicks(4290));

            migrationBuilder.CreateIndex(
                name: "IX_ChargerAdapters_ProtocolId",
                table: "ChargerAdapters",
                column: "ProtocolId");

            migrationBuilder.Sql(@"
                INSERT INTO ChargerAdapters (ChargerId, ProtocolId)
                SELECT c.Id, p.Id
                FROM Chargers c
                CROSS JOIN Protocols p
                WHERE c.Adaptor = 1
                  AND c.IsDeleted = 0
                  AND p.Id <> c.ProtocolId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargerAdapters");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 29, 19, 57, 39, 968, DateTimeKind.Utc).AddTicks(9597));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 29, 19, 57, 39, 975, DateTimeKind.Utc).AddTicks(8606));
        }
    }
}
