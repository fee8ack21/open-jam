using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_legal_consents_legal_documents_legal_document_id",
                table: "user_legal_consents");

            migrationBuilder.DropTable(
                name: "legal_documents");

            migrationBuilder.DropIndex(
                name: "ix_user_legal_consents_legal_document_id",
                table: "user_legal_consents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legal_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_legal_documents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_legal_consents_legal_document_id",
                table: "user_legal_consents",
                column: "legal_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_type_active",
                table: "legal_documents",
                column: "type",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_type_version",
                table: "legal_documents",
                columns: new[] { "type", "version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_user_legal_consents_legal_documents_legal_document_id",
                table: "user_legal_consents",
                column: "legal_document_id",
                principalTable: "legal_documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
