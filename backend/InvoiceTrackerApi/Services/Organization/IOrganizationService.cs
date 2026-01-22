using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;

namespace InvoiceTrackerApi.Services.Organization;

/// <summary>
/// Service interface for Organization business logic
/// </summary>
public interface IOrganizationService
{
    Task<IEnumerable<OrganizationResponse>> GetAllOrganizationsAsync();
    Task<OrganizationResponse> GetOrganizationByIdAsync(int id);
    Task<OrganizationResponse> CreateOrganizationAsync(CreateOrganizationRequest request);
    Task<OrganizationResponse> UpdateOrganizationAsync(int id, UpdateOrganizationRequest request);
    Task DeleteOrganizationAsync(int id);
}
