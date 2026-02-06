namespace InvoiceTrackerApi.Models;

public class Account
{
    public int Id { get; set; }
    public required string BankName { get; set; }
    public required string BranchCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountType { get; set; }
    public int OrganizationId { get; set; }
}
