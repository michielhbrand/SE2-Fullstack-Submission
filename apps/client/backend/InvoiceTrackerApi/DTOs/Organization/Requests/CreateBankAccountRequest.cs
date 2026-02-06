namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateBankAccountRequest
{
    public required string BankName { get; set; }
    public required string BranchCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountType { get; set; }
    public bool Active { get; set; } = true;
}
