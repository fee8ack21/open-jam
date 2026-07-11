using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogStoreFeatured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_store_featured",
                table: "catalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "store_featured_sort_order",
                table: "catalogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_store_featured",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "store_featured_sort_order",
                table: "catalogs");
        }
    }
}
