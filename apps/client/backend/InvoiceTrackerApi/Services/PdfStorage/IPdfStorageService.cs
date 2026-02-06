namespace InvoiceTrackerApi.Services.PdfStorage;

/// <summary>
/// Service interface for PDF storage operations
/// </summary>
public interface IPdfStorageService
{
    /// <summary>
    /// Gets a presigned URL for accessing a PDF stored in MinIO
    /// </summary>
    /// <param name="storageKey">The storage key of the PDF file</param>
    /// <returns>A presigned URL that can be used to access the PDF</returns>
    Task<string> GetPresignedUrlAsync(string storageKey);
}
