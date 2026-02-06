namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateAddressRequest
{
    public required string FirstLine { get; set; }
    public string? SecondLine { get; set; }
    public required string City { get; set; }
    public required string Code { get; set; }
}
