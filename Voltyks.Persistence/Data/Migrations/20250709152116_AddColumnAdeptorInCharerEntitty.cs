using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{ 
    /// <inheritdoc />
    public partial class AddColumnAdeptorInCharerEntitty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_AspNetUsers_UserId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_Capacities_CapacityId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_ChargerAddress_AddressId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_PriceOptions_PriceOptionId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_Protocols_ProtocolId",
                table: "Chargers");

            migrationBuilder.AddColumn<bool>(
                name: "Adeptor",
                table: "Chargers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_AspNetUsers_UserId",
                table: "Chargers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_Capacities_CapacityId",
                table: "Chargers",
                column: "CapacityId",
                principalTable: "Capacities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_ChargerAddress_AddressId",
                table: "Chargers",
                column: "AddressId",
                principalTable: "ChargerAddress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_PriceOptions_PriceOptionId",
                table: "Chargers",
                column: "PriceOptionId",
                principalTable: "PriceOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_Protocols_ProtocolId",
                table: "Chargers",
                column: "ProtocolId",
                principalTable: "Protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_AspNetUsers_UserId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_Capacities_CapacityId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_ChargerAddress_AddressId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_PriceOptions_PriceOptionId",
                table: "Chargers");

            migrationBuilder.DropForeignKey(
                name: "FK_Chargers_Protocols_ProtocolId",
                table: "Chargers");

            migrationBuilder.DropColumn(
                name: "Adeptor",
                table: "Chargers");

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_AspNetUsers_UserId",
                table: "Chargers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_Capacities_CapacityId",
                table: "Chargers",
                column: "CapacityId",
                principalTable: "Capacities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_ChargerAddress_AddressId",
                table: "Chargers",
                column: "AddressId",
                principalTable: "ChargerAddress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_PriceOptions_PriceOptionId",
                table: "Chargers",
                column: "PriceOptionId",
                principalTable: "PriceOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chargers_Protocols_ProtocolId",
                table: "Chargers",
                column: "ProtocolId",
                principalTable: "Protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
