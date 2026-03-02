using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using Shared.Database.Models;

namespace InvoiceTrackerApi.BackgroundServices;

/// <summary>
/// Seeds the two default system templates (Invoice + Quote) on startup if they
/// do not already exist.  These templates have OrganizationId = null, making
/// them visible to every organisation without being owned by any of them.
/// </summary>
public class DefaultTemplateSeedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultTemplateSeedService> _logger;

    private static readonly (string Name, int Version, TemplateType Type, string StorageKey)[] DefaultTemplates =
    [
        ("InvoiceTemplate", 1, TemplateType.Invoice, "invoice-templates/InvoiceTemplate.html"),
        ("QuoteTemplate",   1, TemplateType.Quote,   "quote-templates/QuoteTemplate.html"),
    ];

    private readonly TimeProvider _timeProvider;

    public DefaultTemplateSeedService(
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<DefaultTemplateSeedService> logger)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);

        foreach (var (name, version, type, storageKey) in DefaultTemplates)
        {
            var exists = await db.Templates.AnyAsync(
                t => t.Name == name && t.Version == version && t.OrganizationId == null,
                cancellationToken);

            if (!exists)
            {
                db.Templates.Add(new Template
                {
                    Name        = name,
                    Version     = version,
                    Type        = type,
                    StorageKey  = storageKey,
                    OrganizationId = null,
                    CreatedBy   = "system",
                    Created     = _timeProvider.GetUtcNow().UtcDateTime,
                });

                _logger.LogInformation("Seeding default template: {Name} v{Version} ({Type})",
                    name, version, type);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Default template seed complete");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
