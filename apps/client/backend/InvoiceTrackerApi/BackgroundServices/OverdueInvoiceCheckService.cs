using InvoiceTrackerApi.Services.Invoice;

namespace InvoiceTrackerApi.BackgroundServices;

/// <summary>
/// Daily background service that scans for overdue invoices and publishes
/// invoice-overdue Kafka events to trigger reminder emails.
/// Runs once per day at the configured CheckTimeUtc (default 08:00 UTC).
/// </summary>
public class OverdueInvoiceCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OverdueInvoiceCheckService> _logger;
    private readonly IConfiguration _configuration;

    public OverdueInvoiceCheckService(
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<OverdueInvoiceCheckService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OverdueInvoiceCheckService starting...");

        var initialDelay = GetInitialDelay();
        if (initialDelay > TimeSpan.Zero)
        {
            _logger.LogInformation(
                "Next overdue check scheduled in {Delay} (at {NextRun} UTC)",
                initialDelay, _timeProvider.GetUtcNow().UtcDateTime.Add(initialDelay).ToString("HH:mm"));
            await Task.Delay(initialDelay, stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        do
        {
            await RunCheckAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting scheduled overdue invoice check...");

            using var scope = _serviceProvider.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            var count = await invoiceService.ProcessOverdueInvoicesAsync(null, ct);

            _logger.LogInformation(
                "Scheduled overdue invoice check complete. {Count} invoice(s) queued for reminder.", count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled overdue invoice check");
        }
    }

    private TimeSpan GetInitialDelay()
    {
        var checkTimeStr = _configuration["OverdueInvoice:CheckTimeUtc"] ?? "08:00";

        if (!TimeSpan.TryParse(checkTimeStr, out var checkTime))
            checkTime = new TimeSpan(8, 0, 0);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var nextRun = now.Date.Add(checkTime);

        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }
}
