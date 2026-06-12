using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <summary>
    /// 將既有 email 統一轉為 trim + 小寫；
    /// 此後應用層（UserService.NormalizeEmail）保證新資料一律以小寫寫入。
    /// </summary>
    public partial class NormalizeUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE users SET email = lower(trim(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 原始大小寫無法還原，不做任何事
        }
    }
}
