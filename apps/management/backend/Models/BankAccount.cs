namespace ManagementApi.Models;

public class BankAccount
{
    public int Id { get; set; }
    public required string AccountName { get; set; }
    public required string AccountNumber { get; set; }
    public required string BankName { get; set; }
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public bool Active { get; set; } = true;
    public int OrganizationId { get; set; }
}
