using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legal_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_legal_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_legal_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_legal_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_legal_consents_legal_documents_legal_document_id",
                        column: x => x.legal_document_id,
                        principalTable: "legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_legal_consents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "ix_user_legal_consents_legal_document_id",
                table: "user_legal_consents",
                column: "legal_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_legal_consents_user_id_legal_document_id",
                table: "user_legal_consents",
                columns: new[] { "user_id", "legal_document_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_legal_consents");

            migrationBuilder.DropTable(
                name: "legal_documents");
        }
    }
}
