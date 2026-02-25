using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_Email",
                table: "Clients");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Ensure all existing clients point to a valid organization
            migrationBuilder.Sql("UPDATE \"Clients\" SET \"OrganizationId\" = 1 WHERE \"OrganizationId\" = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Email_OrganizationId",
                table: "Clients",
                columns: new[] { "Email", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_OrganizationId",
                table: "Clients",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Organizations_OrganizationId",
                table: "Clients",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Organizations_OrganizationId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Email_OrganizationId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_OrganizationId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Clients");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Email",
                table: "Clients",
                column: "Email",
                unique: true);
        }
    }
}
