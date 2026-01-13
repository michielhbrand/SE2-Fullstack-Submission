namespace InvoiceTrackerApi.DTOs.Responses;

/// <summary>
/// Generic paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of data being paginated</typeparam>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
