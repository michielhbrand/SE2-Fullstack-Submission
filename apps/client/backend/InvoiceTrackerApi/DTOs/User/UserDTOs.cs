namespace InvoiceTrackerApi.DTOs.User;

/// <summary>
/// Response containing user information from UserDirectory
/// </summary>
public class UserResponse
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Query parameters for UserDirectory
/// </summary>
public class UserDirectoryQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Email";
    public bool SortDescending { get; set; } = false;
    public bool? ActiveOnly { get; set; }
}

/// <summary>
/// Paged response for UserDirectory queries
/// </summary>
public class PagedUserDirectoryResponse
{
    public List<UserResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
