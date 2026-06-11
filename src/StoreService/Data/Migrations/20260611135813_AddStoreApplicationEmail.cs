using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreApplicationEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "store_applications",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "store_applications");
        }
    }
}
