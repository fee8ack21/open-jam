using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmailService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailConfigTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailConfigId = table.Column<int>(type: "integer", nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConfigTranslations_EmailConfigs_EmailConfigId",
                        column: x => x.EmailConfigId,
                        principalTable: "EmailConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EmailConfigs",
                columns: new[] { "Id", "CreatedAt", "Description", "TemplateKey" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "帳號開通驗證信", "email.verification" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "密碼重置信", "email.password_reset" }
                });

            migrationBuilder.InsertData(
                table: "EmailConfigTranslations",
                columns: new[] { "Id", "BodyHtml", "CreatedAt", "EmailConfigId", "Locale", "Subject" },
                values: new object[,]
                {
                    { 1, "<p>感謝您註冊 Open Jam！</p>\n<p>請點擊以下連結驗證您的電子信箱：</p>\n<p><a href=\"{{VerifyUrl}}\" style=\"display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;\">驗證帳號</a></p>\n<p style=\"color:#6b7280;font-size:14px;\">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "zh-TW", "Open Jam 帳號驗證" },
                    { 2, "<p>我們收到了您的密碼重置請求。</p>\n<p>請點擊以下連結重置您的密碼：</p>\n<p><a href=\"{{ResetUrl}}\" style=\"display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;\">重置密碼</a></p>\n<p style=\"color:#6b7280;font-size:14px;\">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "zh-TW", "Open Jam 密碼重置" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigs_TemplateKey",
                table: "EmailConfigs",
                column: "TemplateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigTranslations_EmailConfigId_Locale",
                table: "EmailConfigTranslations",
                columns: new[] { "EmailConfigId", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfigTranslations");

            migrationBuilder.DropTable(
                name: "EmailConfigs");
        }
    }
}
