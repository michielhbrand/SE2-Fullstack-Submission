namespace ManagementApi.Endpoints.Organization;

public static class OrganizationEndpointsMapper
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .RequireAuthorization("SystemAdminOnly")
            .WithTags("Organizations");

        group.MapGetAllOrganizations();
        group.MapGetOrganizationById();
        group.MapCreateOrganization();
        group.MapUpdateOrganization();
        group.MapDeleteOrganization();

        return app;
    }
}
