using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoleToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing string values to enum integers
            // OrgUser = 0, OrgAdmin = 1
            migrationBuilder.Sql(@"
                ALTER TABLE ""OrganizationMembers""
                ALTER COLUMN ""Role"" TYPE integer
                USING (
                    CASE
                        WHEN ""Role"" = 'orgUser' OR ""Role"" = 'OrgUser' THEN 0
                        WHEN ""Role"" = 'orgAdmin' OR ""Role"" = 'OrgAdmin' THEN 1
                        ELSE 0
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert enum integers back to string values
            migrationBuilder.Sql(@"
                ALTER TABLE ""OrganizationMembers""
                ALTER COLUMN ""Role"" TYPE character varying(50)
                USING (
                    CASE
                        WHEN ""Role"" = 0 THEN 'OrgUser'
                        WHEN ""Role"" = 1 THEN 'OrgAdmin'
                        ELSE 'OrgUser'
                    END
                );
            ");
        }
    }
}
