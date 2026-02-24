using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class TemplateIdRefactorAndOrgScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Templates_Name_Version",
                table: "Templates");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Templates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Clear existing string TemplateId values (they can't be cast to int) and convert column type
            migrationBuilder.Sql(
                """
                UPDATE "Quotes" SET "TemplateId" = NULL;
                ALTER TABLE "Quotes" ALTER COLUMN "TemplateId" TYPE integer USING "TemplateId"::integer;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Invoices" SET "TemplateId" = NULL;
                ALTER TABLE "Invoices" ALTER COLUMN "TemplateId" TYPE integer USING "TemplateId"::integer;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Name_Version_OrganizationId",
                table: "Templates",
                columns: new[] { "Name", "Version", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_OrganizationId",
                table: "Templates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_TemplateId",
                table: "Quotes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TemplateId",
                table: "Invoices",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Templates_TemplateId",
                table: "Invoices",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Templates_TemplateId",
                table: "Quotes",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Assign existing templates to the first available organization so the FK constraint can be added
            migrationBuilder.Sql(
                """
                UPDATE "Templates" SET "OrganizationId" = (SELECT "Id" FROM "Organizations" ORDER BY "Id" LIMIT 1)
                WHERE "OrganizationId" = 0;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Templates_TemplateId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Templates_TemplateId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_Name_Version_OrganizationId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_OrganizationId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_TemplateId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TemplateId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Templates");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateId",
                table: "Quotes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateId",
                table: "Invoices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Name_Version",
                table: "Templates",
                columns: new[] { "Name", "Version" },
                unique: true);
        }
    }
}
