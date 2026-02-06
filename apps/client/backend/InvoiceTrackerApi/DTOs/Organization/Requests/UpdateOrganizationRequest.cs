namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class UpdateOrganizationRequest
{
    public string? Name { get; set; }
    public int? AddressId { get; set; }
    public List<int>? BankAccountIds { get; set; }
}
