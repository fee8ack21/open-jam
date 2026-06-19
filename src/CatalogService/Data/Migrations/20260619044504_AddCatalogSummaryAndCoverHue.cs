using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSummaryAndCoverHue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cover_hue",
                table: "catalogs",
                type: "integer",
                nullable: false,
                defaultValue: 256);

            migrationBuilder.AddColumn<string>(
                name: "summary",
                table: "catalogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cover_hue",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "summary",
                table: "catalogs");
        }
    }
}
