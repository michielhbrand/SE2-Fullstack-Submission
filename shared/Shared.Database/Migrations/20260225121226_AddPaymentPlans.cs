using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create PaymentPlans table and seed data BEFORE adding the FK column
            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    MonthlyCostRand = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PaymentPlans",
                columns: new[] { "Id", "MaxUsers", "MonthlyCostRand", "Name" },
                values: new object[,]
                {
                    { 1, 5, 500m, "Basic" },
                    { 2, 15, 2500m, "Advanced" },
                    { 3, -1, 4000m, "Ultimate" }
                });

            // Add column with defaultValue: 1 so all existing orgs are assigned Basic
            migrationBuilder.AddColumn<int>(
                name: "PaymentPlanId",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_PaymentPlanId",
                table: "Organizations",
                column: "PaymentPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_PaymentPlans_PaymentPlanId",
                table: "Organizations",
                column: "PaymentPlanId",
                principalTable: "PaymentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_PaymentPlans_PaymentPlanId",
                table: "Organizations");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_PaymentPlanId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PaymentPlanId",
                table: "Organizations");
        }
    }
}
