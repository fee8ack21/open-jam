using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmailService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "email_config_translations",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "email_config_translations",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "email_configs",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "email_configs",
                keyColumn: "id",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
