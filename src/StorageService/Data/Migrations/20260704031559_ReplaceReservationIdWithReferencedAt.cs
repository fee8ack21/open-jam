using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceReservationIdWithReferencedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reservation_id",
                table: "stored_files");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "referenced_at",
                table: "stored_files",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stored_files_referenced_at",
                table: "stored_files",
                column: "referenced_at");

            // 舊流程於簽發時即建立資產 reference 並計量，既有 Ready 檔案視為已被使用，
            // 否則會被排除於配額之外並遭未使用檔清理排程回收。
            migrationBuilder.Sql(
                "UPDATE stored_files SET referenced_at = created_at WHERE status = 'Ready';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stored_files_referenced_at",
                table: "stored_files");

            migrationBuilder.DropColumn(
                name: "referenced_at",
                table: "stored_files");

            migrationBuilder.AddColumn<Guid>(
                name: "reservation_id",
                table: "stored_files",
                type: "uuid",
                nullable: true);
        }
    }
}
