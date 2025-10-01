using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeesConfigEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPrice",
                table: "ChargingRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "FeesConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinimumFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeesConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "FeesConfigs",
                columns: new[] { "Id", "MinimumFee", "Percentage", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, 40m, 10m, new DateTime(2025, 10, 1, 20, 47, 51, 160, DateTimeKind.Utc).AddTicks(5466), "system" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeesConfigs");

            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "ChargingRequests");
        }
    }
}
