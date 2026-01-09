using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeInvoiceAndQuoteClientData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete existing invoices and quotes since we can't map them to clients
            // (they have duplicate client data that doesn't exist in Clients table)
            migrationBuilder.Sql("DELETE FROM \"InvoiceItems\";");
            migrationBuilder.Sql("DELETE FROM \"Invoices\";");
            migrationBuilder.Sql("DELETE FROM \"QuoteItems\";");
            migrationBuilder.Sql("DELETE FROM \"Quotes\";");

            migrationBuilder.DropColumn(
                name: "ClientAddress",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ClientCellphone",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ClientSurname",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ClientAddress",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientCellphone",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientSurname",
                table: "Invoices");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Quotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ClientId",
                table: "Quotes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ClientId",
                table: "Invoices",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Clients_ClientId",
                table: "Invoices",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Clients_ClientId",
                table: "Quotes",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Clients_ClientId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Clients_ClientId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_ClientId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ClientId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "ClientAddress",
                table: "Quotes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientCellphone",
                table: "Quotes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Quotes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientSurname",
                table: "Quotes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientAddress",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientCellphone",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientSurname",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
