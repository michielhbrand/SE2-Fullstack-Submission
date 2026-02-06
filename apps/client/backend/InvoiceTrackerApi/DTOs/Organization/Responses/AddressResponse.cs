namespace InvoiceTrackerApi.DTOs.Organization.Responses;

public class AddressResponse
{
    public int Id { get; set; }
    public required string FirstLine { get; set; }
    public string? SecondLine { get; set; }
    public required string City { get; set; }
    public required string Code { get; set; }
}
