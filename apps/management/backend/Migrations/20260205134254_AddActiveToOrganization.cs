using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "Organizations");
        }
    }
}
