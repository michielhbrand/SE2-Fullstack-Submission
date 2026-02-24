using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowsAndOrgScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Quotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    QuoteId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflows_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workflows_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Workflows_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workflows_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PerformedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowEvents_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_OrganizationId",
                table: "Quotes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrganizationId",
                table: "Invoices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_OccurredAt",
                table: "WorkflowEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_WorkflowId",
                table: "WorkflowEvents",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_ClientId",
                table: "Workflows",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_InvoiceId",
                table: "Workflows",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_OrganizationId",
                table: "Workflows",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_QuoteId",
                table: "Workflows",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Status",
                table: "Workflows",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Organizations_OrganizationId",
                table: "Invoices",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Organizations_OrganizationId",
                table: "Quotes",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Organizations_OrganizationId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Organizations_OrganizationId",
                table: "Quotes");

            migrationBuilder.DropTable(
                name: "WorkflowEvents");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_OrganizationId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OrganizationId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Invoices");
        }
    }
}
