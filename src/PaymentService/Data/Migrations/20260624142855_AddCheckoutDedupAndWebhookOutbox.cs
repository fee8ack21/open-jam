using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutDedupAndWebhookOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "raw_payload",
                table: "provider_events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "checkout_url",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expired_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id_pending",
                table: "payments",
                column: "order_id",
                unique: true,
                filter: "status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_order_id_pending",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "raw_payload",
                table: "provider_events");

            migrationBuilder.DropColumn(
                name: "checkout_url",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "expired_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "payments");
        }
    }
}
