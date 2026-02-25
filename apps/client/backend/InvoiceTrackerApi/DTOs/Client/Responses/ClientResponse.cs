namespace InvoiceTrackerApi.DTOs.Client.Responses;

/// <summary>
/// Client data transfer object for API responses
/// </summary>
public class ClientResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cellphone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsCompany { get; set; }
    public string? VatNumber { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? KeycloakUserId { get; set; }
}
