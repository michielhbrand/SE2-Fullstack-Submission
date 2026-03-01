using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class MakeTemplateOrganizationIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates");

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Templates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates");

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Templates",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
