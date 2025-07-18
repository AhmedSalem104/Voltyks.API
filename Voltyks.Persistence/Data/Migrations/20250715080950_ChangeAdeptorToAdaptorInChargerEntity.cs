using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAdeptorToAdaptorInChargerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Adeptor",
                table: "Chargers",
                newName: "Adaptor");

            migrationBuilder.RenameColumn(
                name: "KW",
                table: "Capacities",
                newName: "kw");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Adaptor",
                table: "Chargers",
                newName: "Adeptor");

            migrationBuilder.RenameColumn(
                name: "kw",
                table: "Capacities",
                newName: "KW");
        }
    }
}
