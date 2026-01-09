namespace PdfGeneratorService.Services.Storage;

public interface IMinioStorageService
{
    Task<string> UploadPdfAsync(int invoiceId, byte[] pdfBytes);
    Task<string> UploadQuotePdfAsync(int quoteId, byte[] pdfBytes);
    Task<string> UploadTemplateAsync(string templateName, string htmlContent);
    Task<string> UploadQuoteTemplateAsync(string templateName, string htmlContent);
    Task<List<string>> ListTemplatesAsync();
    Task<List<string>> ListQuoteTemplatesAsync();
    Task<string> GetTemplateAsync(string templateName);
    Task<string> GetQuoteTemplateAsync(string templateName);
    Task EnsureBucketsExistAsync();
    Task<string> GetPresignedUrlAsync(string storageKey, int expiryInSeconds = 3600);
}