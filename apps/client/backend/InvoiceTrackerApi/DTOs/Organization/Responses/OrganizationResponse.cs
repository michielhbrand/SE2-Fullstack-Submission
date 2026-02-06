namespace InvoiceTrackerApi.DTOs.Organization.Responses;

public class OrganizationResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required AddressResponse Address { get; set; }
    public List<BankAccountResponse> BankAccounts { get; set; } = new();
}
