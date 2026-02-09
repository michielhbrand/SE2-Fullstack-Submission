using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateBankAccountRequest
{
    [Required(ErrorMessage = "Bank name is required")]
    [MaxLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
    public required string BankName { get; set; }
    
    [Required(ErrorMessage = "Branch code is required")]
    [MaxLength(20, ErrorMessage = "Branch code cannot exceed 20 characters")]
    public required string BranchCode { get; set; }
    
    [Required(ErrorMessage = "Account number is required")]
    [MaxLength(50, ErrorMessage = "Account number cannot exceed 50 characters")]
    public required string AccountNumber { get; set; }
    
    [Required(ErrorMessage = "Account type is required")]
    [MaxLength(50, ErrorMessage = "Account type cannot exceed 50 characters")]
    public required string AccountType { get; set; }
    
    public bool Active { get; set; } = true;
}
