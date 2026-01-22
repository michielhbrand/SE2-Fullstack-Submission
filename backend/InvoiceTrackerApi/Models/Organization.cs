namespace InvoiceTrackerApi.Models;

public class Organization
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public List<string> UserIds { get; set; } = new();
    public List<int> BankAccountIds { get; set; } = new();
}
