namespace ManagementApi.Endpoints.Organization;

public static class OrganizationEndpointsConfiguration
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organizations")
            .RequireAuthorization("SystemAdminOnly")
            .WithTags("Organizations");

        group.MapGetAllOrganizations();
        group.MapGetOrganizationById();
        group.MapCreateOrganization();
        group.MapUpdateOrganization();
        group.MapSeedDemoData();

        return app;
    }
}
