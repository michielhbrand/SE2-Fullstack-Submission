using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceTrackerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMembersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove UserIds column from Organizations table
            migrationBuilder.DropColumn(
                name: "UserIds",
                table: "Organizations");

            // Create OrganizationMembers join table
            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => new { x.OrganizationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for efficient queries
            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId",
                table: "OrganizationMembers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_UserId",
                table: "OrganizationMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop OrganizationMembers table
            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            // Re-add UserIds column to Organizations table
            migrationBuilder.AddColumn<string>(
                name: "UserIds",
                table: "Organizations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
