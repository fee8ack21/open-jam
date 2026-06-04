using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmailService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_config_translations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email_config_id = table.Column<int>(type: "integer", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_config_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_config_translations_email_configs_email_config_id",
                        column: x => x.email_config_id,
                        principalTable: "email_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "email_configs",
                columns: new[] { "id", "created_at", "description", "template_key" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "帳號開通驗證信", "email.verification" },
                    { 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "密碼重置信", "email.password_reset" }
                });

            migrationBuilder.InsertData(
                table: "email_config_translations",
                columns: new[] { "id", "body_html", "created_at", "email_config_id", "locale", "subject" },
                values: new object[,]
                {
                    { 1, "<p>感謝您註冊 Open Jam！</p>\n<p>請點擊以下連結驗證您的電子信箱：</p>\n<p><a href=\"{{VerifyUrl}}\" style=\"display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;\">驗證帳號</a></p>\n<p style=\"color:#6b7280;font-size:14px;\">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "zh-TW", "Open Jam 帳號驗證" },
                    { 2, "<p>我們收到了您的密碼重置請求。</p>\n<p>請點擊以下連結重置您的密碼：</p>\n<p><a href=\"{{ResetUrl}}\" style=\"display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;\">重置密碼</a></p>\n<p style=\"color:#6b7280;font-size:14px;\">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "zh-TW", "Open Jam 密碼重置" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_config_translations_email_config_id_locale",
                table: "email_config_translations",
                columns: new[] { "email_config_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_configs_template_key",
                table: "email_configs",
                column: "template_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_records_outbox_message_id",
                table: "email_records",
                column: "outbox_message_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_config_translations");

            migrationBuilder.DropTable(
                name: "email_records");

            migrationBuilder.DropTable(
                name: "email_configs");
        }
    }
}
