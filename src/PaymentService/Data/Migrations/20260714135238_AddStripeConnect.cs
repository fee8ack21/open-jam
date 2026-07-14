using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "application_fee_amount",
                table: "payments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "destination_account_id",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                table: "payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "connected_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    details_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    charges_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    payouts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connected_accounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_store_id",
                table: "payments",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_connected_accounts_store_id",
                table: "connected_accounts",
                column: "store_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_connected_accounts_stripe_account_id",
                table: "connected_accounts",
                column: "stripe_account_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connected_accounts");

            migrationBuilder.DropIndex(
                name: "ix_payments_store_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "application_fee_amount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "destination_account_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "payments");
        }
    }
}
