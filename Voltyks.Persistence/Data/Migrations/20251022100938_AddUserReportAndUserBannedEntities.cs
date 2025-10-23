using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReportAndUserBannedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UserShouldBeBanned",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReports_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReports_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsersBanneds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BannedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BanExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BanReason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersBanneds", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 22, 10, 9, 36, 37, DateTimeKind.Utc).AddTicks(3701));

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ProcessId",
                table: "UserReports",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_UserId",
                table: "UserReports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReports");

            migrationBuilder.DropTable(
                name: "UsersBanneds");

            migrationBuilder.DropColumn(
                name: "UserShouldBeBanned",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 10, 20, 14, 23, 53, 569, DateTimeKind.Utc).AddTicks(3036));
        }
    }
}
