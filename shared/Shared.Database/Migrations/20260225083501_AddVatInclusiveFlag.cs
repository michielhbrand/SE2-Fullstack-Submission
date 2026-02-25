using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddVatInclusiveFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VatInclusive",
                table: "Quotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VatInclusive",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatInclusive",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "VatInclusive",
                table: "Invoices");
        }
    }
}
