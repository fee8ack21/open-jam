using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalogs_store_id_slug",
                table: "catalogs");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "catalogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "catalogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_store_id_slug",
                table: "catalogs",
                columns: new[] { "store_id", "slug" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalogs_store_id_slug",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "catalogs");

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_store_id_slug",
                table: "catalogs",
                columns: new[] { "store_id", "slug" },
                unique: true);
        }
    }
}
