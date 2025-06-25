using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChargerCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargerAddress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BuildingNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargerAddress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capacities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KW = table.Column<int>(type: "int", nullable: false),
                    PriceOptionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capacities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capacities_PriceOptions_PriceOptionId",
                        column: x => x.PriceOptionId,
                        principalTable: "PriceOptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Chargers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProtocolId = table.Column<int>(type: "int", nullable: false),
                    CapacityId = table.Column<int>(type: "int", nullable: false),
                    PriceOptionId = table.Column<int>(type: "int", nullable: false),
                    AddressId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chargers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chargers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chargers_Capacities_CapacityId",
                        column: x => x.CapacityId,
                        principalTable: "Capacities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chargers_ChargerAddress_AddressId",
                        column: x => x.AddressId,
                        principalTable: "ChargerAddress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chargers_PriceOptions_PriceOptionId",
                        column: x => x.PriceOptionId,
                        principalTable: "PriceOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chargers_Protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capacities_PriceOptionId",
                table: "Capacities",
                column: "PriceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_AddressId",
                table: "Chargers",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_CapacityId",
                table: "Chargers",
                column: "CapacityId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_PriceOptionId",
                table: "Chargers",
                column: "PriceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_ProtocolId",
                table: "Chargers",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargers_UserId",
                table: "Chargers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chargers");

            migrationBuilder.DropTable(
                name: "Capacities");

            migrationBuilder.DropTable(
                name: "ChargerAddress");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "PriceOptions");
        }
    }
}
