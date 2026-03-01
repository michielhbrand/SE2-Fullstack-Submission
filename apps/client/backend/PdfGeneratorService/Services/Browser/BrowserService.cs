using PuppeteerSharp;

namespace PdfGeneratorService.Services.Browser;

/// <summary>
/// Singleton that owns a single persistent Chromium browser process.
/// All PDF generations share the same browser; only individual pages are
/// created and disposed per request.  This eliminates the ~5-15 second
/// cold-launch cost that would otherwise be paid for every PDF.
/// The browser is re-launched automatically if it exits unexpectedly.
/// </summary>
public class BrowserService : IBrowserService, IAsyncDisposable
{
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<BrowserService> _logger;

    public BrowserService(ILogger<BrowserService> logger)
    {
        _logger = logger;
    }

    public async Task<IPage> AcquirePageAsync()
    {
        // Fast path: browser already running
        if (_browser != null && _browser.IsConnected)
            return await _browser.NewPageAsync();

        // Slow path: first launch or recovery after crash
        await _lock.WaitAsync();
        try
        {
            if (_browser == null || !_browser.IsConnected)
                await LaunchBrowserAsync();

            return await _browser!.NewPageAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LaunchBrowserAsync()
    {
        var executablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");

        if (string.IsNullOrEmpty(executablePath))
        {
            _logger.LogInformation("PUPPETEER_EXECUTABLE_PATH not set — downloading Chromium...");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }

        _logger.LogInformation("Launching persistent Chromium browser...");

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = string.IsNullOrEmpty(executablePath) ? null : executablePath,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        });

        _browser.Disconnected += (_, _) =>
            _logger.LogWarning("Chromium browser disconnected — will re-launch on next request");

        _logger.LogInformation("Chromium browser launched successfully");
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }
        _lock.Dispose();
    }
}
