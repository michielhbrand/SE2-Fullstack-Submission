namespace InvoiceTrackerApi.DTOs.Invoice.Responses;

/// <summary>
/// Response for the POST /api/invoice/process-overdue endpoint
/// </summary>
public class ProcessOverdueResponse
{
    public int QueuedCount { get; set; }
}
