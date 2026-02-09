using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoleFromOrganizationMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "OrganizationMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "OrganizationMembers",
                type: "integer",
                maxLength: 50,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountName = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    BranchCode = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    SwiftCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_OrganizationId",
                table: "BankAccounts",
                column: "OrganizationId");
        }
    }
}
