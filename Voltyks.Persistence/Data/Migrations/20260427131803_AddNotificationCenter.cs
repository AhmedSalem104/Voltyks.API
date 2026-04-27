using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voltyks.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationBroadcasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AudienceJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    DbPersistedCount = table.Column<int>(type: "int", nullable: false),
                    FcmAttemptedCount = table.Column<int>(type: "int", nullable: false),
                    FcmSucceededCount = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationBroadcasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BodyAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiredParamsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCustomizable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Key);
                });

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 27, 13, 18, 2, 343, DateTimeKind.Utc).AddTicks(469));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 27, 13, 18, 2, 350, DateTimeKind.Utc).AddTicks(7596));

            migrationBuilder.CreateIndex(
                name: "IX_NotificationBroadcasts_AdminUserId",
                table: "NotificationBroadcasts",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationBroadcasts_SentAt",
                table: "NotificationBroadcasts",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationBroadcasts");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 20, 19, 8, 43, 616, DateTimeKind.Utc).AddTicks(8139));

            migrationBuilder.UpdateData(
                table: "FeesConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 20, 19, 8, 43, 624, DateTimeKind.Utc).AddTicks(3383));
        }
    }
}
