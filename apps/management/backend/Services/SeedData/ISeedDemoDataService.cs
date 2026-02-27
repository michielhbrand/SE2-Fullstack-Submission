namespace ManagementApi.Services.SeedData;

public interface ISeedDemoDataService
{
    Task SeedDemoDataAsync(int organizationId, CancellationToken ct = default);
}
