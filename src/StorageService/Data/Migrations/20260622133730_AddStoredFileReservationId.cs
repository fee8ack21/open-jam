using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredFileReservationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "reservation_id",
                table: "stored_files",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reservation_id",
                table: "stored_files");
        }
    }
}
