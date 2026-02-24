namespace InvoiceTrackerApi.DTOs.Organization.Responses;

public class AddressResponse
{
    public int Id { get; set; }
    public required string Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
