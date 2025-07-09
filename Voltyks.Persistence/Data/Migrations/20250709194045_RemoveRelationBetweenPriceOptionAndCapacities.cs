using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRelationBetweenPriceOptionAndCapacities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Capacities_PriceOptions_PriceOptionId",
                table: "Capacities");

            migrationBuilder.DropIndex(
                name: "IX_Capacities_PriceOptionId",
                table: "Capacities");

            migrationBuilder.DropColumn(
                name: "PriceOptionId",
                table: "Capacities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceOptionId",
                table: "Capacities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capacities_PriceOptionId",
                table: "Capacities",
                column: "PriceOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Capacities_PriceOptions_PriceOptionId",
                table: "Capacities",
                column: "PriceOptionId",
                principalTable: "PriceOptions",
                principalColumn: "Id");
        }
    }
}
