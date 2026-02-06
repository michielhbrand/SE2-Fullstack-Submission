using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceTrackerApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameAmountToQuantityInQuoteItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "QuoteItems",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "InvoiceItems",
                newName: "Quantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "QuoteItems",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "InvoiceItems",
                newName: "Amount");
        }
    }
}
