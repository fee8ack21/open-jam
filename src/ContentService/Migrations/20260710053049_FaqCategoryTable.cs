using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Migrations
{
    /// <inheritdoc />
    public partial class FaqCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_faq_items_category_sort_order",
                table: "faq_items");

            migrationBuilder.DropColumn(
                name: "category",
                table: "faq_items");

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "faq_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "faq_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faq_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_faq_items_category_id_sort_order",
                table: "faq_items",
                columns: new[] { "category_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_faq_categories_slug",
                table: "faq_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_faq_categories_sort_order",
                table: "faq_categories",
                column: "sort_order");

            migrationBuilder.AddForeignKey(
                name: "fk_faq_items_faq_categories_category_id",
                table: "faq_items",
                column: "category_id",
                principalTable: "faq_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_faq_items_faq_categories_category_id",
                table: "faq_items");

            migrationBuilder.DropTable(
                name: "faq_categories");

            migrationBuilder.DropIndex(
                name: "ix_faq_items_category_id_sort_order",
                table: "faq_items");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "faq_items");

            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "faq_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_faq_items_category_sort_order",
                table: "faq_items",
                columns: new[] { "category", "sort_order" });
        }
    }
}
