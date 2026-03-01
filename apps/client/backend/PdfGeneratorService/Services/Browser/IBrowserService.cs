using PuppeteerSharp;

namespace PdfGeneratorService.Services.Browser;

public interface IBrowserService
{
    /// <summary>
    /// Returns a new page from the shared persistent browser.
    /// The caller is responsible for closing the page after use.
    /// </summary>
    Task<IPage> AcquirePageAsync();
}
