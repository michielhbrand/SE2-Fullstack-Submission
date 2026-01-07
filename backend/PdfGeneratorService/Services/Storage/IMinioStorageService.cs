namespace PdfGeneratorService.Services.Storage;

public interface IMinioStorageService
{
    Task<string> UploadPdfAsync(int invoiceId, byte[] pdfBytes);
    Task<string> UploadTemplateAsync(string templateName, string htmlContent);
    Task<List<string>> ListTemplatesAsync();
    Task<string> GetTemplateAsync(string templateName);
    Task EnsureBucketsExistAsync();
    Task<string> GetPresignedUrlAsync(string storageKey, int expiryInSeconds = 3600);
}