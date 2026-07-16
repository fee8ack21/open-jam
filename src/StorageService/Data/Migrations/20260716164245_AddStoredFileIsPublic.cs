using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredFileIsPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "stored_files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 舊制以 public/ 鍵值前綴標記公開物件，回填旗標（舊鍵值不搬遷，僅供保留期內清理路由）。
            migrationBuilder.Sql("UPDATE stored_files SET is_public = TRUE WHERE storage_key LIKE 'public/%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_public",
                table: "stored_files");
        }
    }
}
