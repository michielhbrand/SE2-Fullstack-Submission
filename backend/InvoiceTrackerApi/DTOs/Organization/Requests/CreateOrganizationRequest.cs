namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateOrganizationRequest
{
    public required string Name { get; set; }
    public required CreateAddressRequest Address { get; set; }
    public List<string> UserIds { get; set; } = new();
}
