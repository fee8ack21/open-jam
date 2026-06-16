using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_tag_mappings",
                columns: table => new
                {
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_tag_mappings", x => new { x.catalog_id, x.tag_id });
                });

            migrationBuilder.CreateTable(
                name: "catalog_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    usage_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_version_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_version_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    release_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalogs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    thumbnail_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_assets_catalog_id",
                table: "catalog_assets",
                column: "catalog_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_categories_parent_id",
                table: "catalog_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_categories_slug",
                table: "catalog_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_tag_mappings_tag_id",
                table: "catalog_tag_mappings",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_tags_name",
                table: "catalog_tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_version_assets_catalog_version_id",
                table: "catalog_version_assets",
                column: "catalog_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_versions_catalog_id_version",
                table: "catalog_versions",
                columns: new[] { "catalog_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_category_id",
                table: "catalogs",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_status",
                table: "catalogs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_store_id",
                table: "catalogs",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_store_id_slug",
                table: "catalogs",
                columns: new[] { "store_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at",
                table: "outbox_messages",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_assets");

            migrationBuilder.DropTable(
                name: "catalog_categories");

            migrationBuilder.DropTable(
                name: "catalog_tag_mappings");

            migrationBuilder.DropTable(
                name: "catalog_tags");

            migrationBuilder.DropTable(
                name: "catalog_version_assets");

            migrationBuilder.DropTable(
                name: "catalog_versions");

            migrationBuilder.DropTable(
                name: "catalogs");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
