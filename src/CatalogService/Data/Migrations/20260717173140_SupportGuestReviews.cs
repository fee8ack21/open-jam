using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportGuestReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalog_reviews_catalog_id_reviewer_user_id",
                table: "catalog_reviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "reviewer_user_id",
                table: "catalog_reviews",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "reviewer_email",
                table: "catalog_reviews",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_reviews_catalog_id_reviewer_email",
                table: "catalog_reviews",
                columns: new[] { "catalog_id", "reviewer_email" },
                unique: true,
                filter: "reviewer_email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_reviews_catalog_id_reviewer_user_id",
                table: "catalog_reviews",
                columns: new[] { "catalog_id", "reviewer_user_id" },
                unique: true,
                filter: "reviewer_user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalog_reviews_catalog_id_reviewer_email",
                table: "catalog_reviews");

            migrationBuilder.DropIndex(
                name: "ix_catalog_reviews_catalog_id_reviewer_user_id",
                table: "catalog_reviews");

            migrationBuilder.DropColumn(
                name: "reviewer_email",
                table: "catalog_reviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "reviewer_user_id",
                table: "catalog_reviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_reviews_catalog_id_reviewer_user_id",
                table: "catalog_reviews",
                columns: new[] { "catalog_id", "reviewer_user_id" },
                unique: true);
        }
    }
}
