namespace PdfGeneratorService.Services.Storage;

public interface IMinioStorageService
{
    Task<string> UploadPdfAsync(int invoiceId, byte[] pdfBytes);
}