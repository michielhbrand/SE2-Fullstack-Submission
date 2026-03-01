using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using Shared.Database.Models;

namespace ManagementApi.Services.SeedData;

public class SeedDemoDataService : ISeedDemoDataService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedDemoDataService> _logger;

    private static readonly string[] ServiceDescriptions =
    [
        "IT Infrastructure Setup and Configuration",
        "Monthly Retainer - Software Development",
        "Web Application Development",
        "Cloud Migration Consulting",
        "Annual Software License",
        "Network Security Audit",
        "Digital Marketing Campaign Management",
        "Business Process Automation",
        "Data Analytics and Reporting Dashboard",
        "Legal Compliance Review",
        "Quarterly Maintenance and Support",
        "Employee Training - Microsoft 365",
        "Project Management (per day)",
        "Interior Design Consultation",
        "Architectural Drawing and Review",
    ];

    public SeedDemoDataService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SeedDemoDataService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedDemoDataAsync(int organizationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding demo data for organization {OrganizationId}", organizationId);

        var clients = await CreateClientsAsync(organizationId, ct);
        var now = DateTime.UtcNow;

        // Collect PDF generation tasks from all scenarios but do NOT await them yet —
        // all DB records are committed before any task starts, so there is no race.
        var pdfTasks = new List<Task>();

        await SeedScenarioAAsync(organizationId, clients, now, ct, pdfTasks);
        await SeedScenarioBAsync(organizationId, clients, now, ct, pdfTasks);
        await SeedScenarioCAsync(organizationId, clients, now, ct, pdfTasks);
        await SeedScenarioDAsync(organizationId, clients, now, ct, pdfTasks);

        // Run all PDF generations in parallel, capped at 5 concurrent requests so
        // Chromium doesn't open too many pages at once and exhaust Docker memory.
        _logger.LogInformation("Generating {Count} PDFs in parallel (max 5 concurrent)...", pdfTasks.Count);
        var semaphore = new SemaphoreSlim(5, 5);
        var throttledTasks = pdfTasks.Select(async t =>
        {
            await semaphore.WaitAsync(ct);
            try { await t; }
            finally { semaphore.Release(); }
        });
        await Task.WhenAll(throttledTasks);

        _logger.LogInformation("Demo data seeding complete for organization {OrganizationId}", organizationId);
    }

    // -------------------------------------------------------------------------
    // Clients
    // -------------------------------------------------------------------------

    private async Task<List<Client>> CreateClientsAsync(int organizationId, CancellationToken ct)
    {
        var clients = new List<Client>
        {
            new() { Name = "TechVision Solutions (Pty) Ltd",     Email = "accounts@techvision.co.za",   Cellphone = "0821234567", IsCompany = true,  VatNumber = "4123456789", OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Innovate Digital SA (Pty) Ltd",      Email = "billing@innovatesa.com",      Cellphone = "0829876543", IsCompany = true,  VatNumber = "4987654321", OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Cape Construction & Engineering",     Email = "finance@capeconst.co.za",     Cellphone = "0832345678", IsCompany = true,  VatNumber = "4234567890", OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Johannesburg Financial Advisors",     Email = "admin@jbgfinance.co.za",      Cellphone = "0843456789", IsCompany = true,  VatNumber = "4345678901", OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Pretoria Marketing Agency (Pty) Ltd", Email = "accounts@ptamarketing.co.za", Cellphone = "0717654321", IsCompany = true,  VatNumber = "4456789012", OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Eastern Cape Logistics",              Email = "billing@eclogistics.co.za",   Cellphone = "0729876543", IsCompany = true,  VatNumber = "4567890123", OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Thabo Molefe",                        Email = "thabo.molefe@gmail.com",      Cellphone = "0761234567", IsCompany = false, VatNumber = null,         OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Sarah van der Merwe",                 Email = "sarah.vdm@outlook.com",       Cellphone = "0798765432", IsCompany = false, VatNumber = null,         OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Lindiwe Nkosi",                       Email = "lindiwe.nkosi@icloud.com",    Cellphone = "0782345678", IsCompany = false, VatNumber = null,         OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
            new() { Name = "Pieter Botha",                        Email = "pieter.botha@yahoo.com",      Cellphone = "0773456789", IsCompany = false, VatNumber = null,         OrganizationId = organizationId, DateCreated = DateTime.UtcNow },
        };

        _db.Clients.AddRange(clients);
        await _db.SaveChangesAsync(ct);
        return clients;
    }

    // -------------------------------------------------------------------------
    // Scenario A — 8 Paid Workflows (revenue growth over 6 months)
    // -------------------------------------------------------------------------

    private async Task SeedScenarioAAsync(int orgId, List<Client> clients, DateTime now, CancellationToken ct, List<Task> pdfTasks)
    {
        var scenarios = new (Client Client, int DaysAgo, decimal Amount, string Type)[]
        {
            (clients[0], -180, 82_500m,  WorkflowType.InvoiceFirst),
            (clients[1], -175, 78_000m,  WorkflowType.QuoteFirst),
            (clients[2], -150, 95_000m,  WorkflowType.InvoiceFirst),
            (clients[3], -120, 110_000m, WorkflowType.QuoteFirst),
            (clients[4],  -90, 125_000m, WorkflowType.InvoiceFirst),
            (clients[5],  -60, 145_000m, WorkflowType.QuoteFirst),
            (clients[6],  -40, 65_000m,  WorkflowType.InvoiceFirst),
            (clients[7],  -35, 125_000m, WorkflowType.QuoteFirst),
        };

        foreach (var s in scenarios)
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(s.DaysAgo), DateTimeKind.Utc);
            if (s.Type == WorkflowType.InvoiceFirst)
                await CreateInvoiceFirstPaidAsync(orgId, s.Client, createdAt, s.Amount, ct, pdfTasks);
            else
                await CreateQuoteFirstPaidAsync(orgId, s.Client, createdAt, s.Amount, ct, pdfTasks);
        }
    }

    private async Task CreateInvoiceFirstPaidAsync(int orgId, Client client, DateTime createdAt, decimal amount, CancellationToken ct, List<Task> pdfTasks)
    {
        var invoice = new Invoice
        {
            ClientId       = client.Id,
            OrganizationId = orgId,
            DateCreated    = createdAt,
            PayByDate      = createdAt.AddDays(30),
            VatInclusive   = true,
        };
        foreach (var item in CreateInvoiceItems(amount, 3))
            invoice.Items.Add(item);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        pdfTasks.Add(GenerateInvoicePdfAsync(invoice.Id, ct));

        var workflow = new Workflow
        {
            OrganizationId = orgId,
            ClientId       = client.Id,
            Status         = WorkflowStatus.Paid,
            Type           = WorkflowType.InvoiceFirst,
            InvoiceId      = invoice.Id,
            CreatedAt      = createdAt,
            UpdatedAt      = createdAt.AddDays(20),
            Events         = new List<WorkflowEvent>
            {
                new() { EventType = WorkflowEventType.InvoiceCreated, OccurredAt = createdAt,             PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.SentForPayment, OccurredAt = createdAt.AddDays(1),  PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.MarkedAsPaid,   OccurredAt = createdAt.AddDays(20), PerformedBy = "Demo Seed" },
            },
        };
        _db.Workflows.Add(workflow);
        await _db.SaveChangesAsync(ct);
    }

    private async Task CreateQuoteFirstPaidAsync(int orgId, Client client, DateTime createdAt, decimal amount, CancellationToken ct, List<Task> pdfTasks)
    {
        var quote = new Quote
        {
            ClientId       = client.Id,
            OrganizationId = orgId,
            DateCreated    = createdAt,
            VatInclusive   = true,
        };
        foreach (var item in CreateQuoteItems(amount, 3))
            quote.Items.Add(item);
        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync(ct);
        pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

        var invoiceDate = createdAt.AddDays(5);
        var invoice = new Invoice
        {
            ClientId       = client.Id,
            OrganizationId = orgId,
            DateCreated    = invoiceDate,
            PayByDate      = invoiceDate.AddDays(30),
            VatInclusive   = true,
        };
        foreach (var item in CreateInvoiceItems(amount, 3))
            invoice.Items.Add(item);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        pdfTasks.Add(GenerateInvoicePdfAsync(invoice.Id, ct));

        var workflow = new Workflow
        {
            OrganizationId = orgId,
            ClientId       = client.Id,
            Status         = WorkflowStatus.Paid,
            Type           = WorkflowType.QuoteFirst,
            QuoteId        = quote.Id,
            InvoiceId      = invoice.Id,
            CreatedAt      = createdAt,
            UpdatedAt      = createdAt.AddDays(25),
            Events         = new List<WorkflowEvent>
            {
                new() { EventType = WorkflowEventType.QuoteCreated,       OccurredAt = createdAt,             PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.SentForApproval,    OccurredAt = createdAt.AddDays(1),  PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.Approved,           OccurredAt = createdAt.AddDays(3),  PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.ConvertedToInvoice, OccurredAt = createdAt.AddDays(5),  PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.SentForPayment,     OccurredAt = createdAt.AddDays(6),  PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.MarkedAsPaid,       OccurredAt = createdAt.AddDays(25), PerformedBy = "Demo Seed" },
            },
        };
        _db.Workflows.Add(workflow);
        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Scenario B — 3 Overdue Workflows
    // -------------------------------------------------------------------------

    private async Task SeedScenarioBAsync(int orgId, List<Client> clients, DateTime now, CancellationToken ct, List<Task> pdfTasks)
    {
        var scenarios = new (Client Client, int DaysOverdue, decimal Amount, bool OverdueReminder)[]
        {
            (clients[8], 45, 35_000m, false),
            (clients[9], 20, 48_000m, false),
            (clients[0], 60, 62_000m, true),
        };

        foreach (var s in scenarios)
        {
            var invoiceCreatedAt = DateTime.SpecifyKind(now.AddDays(-(s.DaysOverdue + 30)), DateTimeKind.Utc);
            var payByDate        = DateTime.SpecifyKind(now.AddDays(-s.DaysOverdue), DateTimeKind.Utc);

            var invoice = new Invoice
            {
                ClientId       = s.Client.Id,
                OrganizationId = orgId,
                DateCreated    = invoiceCreatedAt,
                PayByDate      = payByDate,
                VatInclusive   = true,
            };
            foreach (var item in CreateInvoiceItems(s.Amount, 2))
                invoice.Items.Add(item);
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateInvoicePdfAsync(invoice.Id, ct));

            var events = new List<WorkflowEvent>
            {
                new() { EventType = WorkflowEventType.InvoiceCreated, OccurredAt = invoiceCreatedAt,            PerformedBy = "Demo Seed" },
                new() { EventType = WorkflowEventType.SentForPayment, OccurredAt = invoiceCreatedAt.AddDays(1), PerformedBy = "Demo Seed" },
            };
            if (s.OverdueReminder)
                events.Add(new WorkflowEvent { EventType = WorkflowEventType.OverdueReminderSent, OccurredAt = payByDate.AddDays(15), PerformedBy = "Demo Seed" });

            var workflow = new Workflow
            {
                OrganizationId = orgId,
                ClientId       = s.Client.Id,
                Status         = WorkflowStatus.SentForPayment,
                Type           = WorkflowType.InvoiceFirst,
                InvoiceId      = invoice.Id,
                CreatedAt      = invoiceCreatedAt,
                UpdatedAt      = invoiceCreatedAt.AddDays(1),
                Events         = events,
            };
            _db.Workflows.Add(workflow);
            await _db.SaveChangesAsync(ct);
        }
    }

    // -------------------------------------------------------------------------
    // Scenario C — 6 In-Progress Workflows
    // -------------------------------------------------------------------------

    private async Task SeedScenarioCAsync(int orgId, List<Client> clients, DateTime now, CancellationToken ct, List<Task> pdfTasks)
    {
        // 1. Cape Construction — Draft (QuoteFirst)
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-2), DateTimeKind.Utc);
            var quote = new Quote { ClientId = clients[2].Id, OrganizationId = orgId, DateCreated = createdAt, VatInclusive = true };
            foreach (var item in CreateQuoteItems(45_000m, 2)) quote.Items.Add(item);
            _db.Quotes.Add(quote);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[2].Id,
                Status         = WorkflowStatus.Draft,
                Type           = WorkflowType.QuoteFirst,
                QuoteId        = quote.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.QuoteCreated, OccurredAt = createdAt, PerformedBy = "Demo Seed" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }

        // 2. JHB Financial — PendingApproval (QuoteFirst)
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-5), DateTimeKind.Utc);
            var quote = new Quote { ClientId = clients[3].Id, OrganizationId = orgId, DateCreated = createdAt, VatInclusive = true };
            foreach (var item in CreateQuoteItems(35_000m, 2)) quote.Items.Add(item);
            _db.Quotes.Add(quote);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[3].Id,
                Status         = WorkflowStatus.PendingApproval,
                Type           = WorkflowType.QuoteFirst,
                QuoteId        = quote.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.QuoteCreated,    OccurredAt = createdAt,            PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.SentForApproval, OccurredAt = createdAt.AddDays(1), PerformedBy = "Demo Seed" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }

        // 3. PTA Marketing — Approved (QuoteFirst)
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-8), DateTimeKind.Utc);
            var quote = new Quote { ClientId = clients[4].Id, OrganizationId = orgId, DateCreated = createdAt, VatInclusive = true };
            foreach (var item in CreateQuoteItems(68_000m, 2)) quote.Items.Add(item);
            _db.Quotes.Add(quote);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[4].Id,
                Status         = WorkflowStatus.Approved,
                Type           = WorkflowType.QuoteFirst,
                QuoteId        = quote.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.QuoteCreated,    OccurredAt = createdAt,            PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.SentForApproval, OccurredAt = createdAt.AddDays(1), PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.Approved,        OccurredAt = createdAt.AddDays(3), PerformedBy = "Demo Seed" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }

        // 4. Eastern Cape Logistics — Rejected with QuoteModified (QuoteFirst)
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-10), DateTimeKind.Utc);
            var quote = new Quote { ClientId = clients[5].Id, OrganizationId = orgId, DateCreated = createdAt, VatInclusive = true };
            foreach (var item in CreateQuoteItems(52_000m, 2)) quote.Items.Add(item);
            _db.Quotes.Add(quote);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[5].Id,
                Status         = WorkflowStatus.Rejected,
                Type           = WorkflowType.QuoteFirst,
                QuoteId        = quote.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.QuoteCreated,    OccurredAt = createdAt,            PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.SentForApproval, OccurredAt = createdAt.AddDays(1), PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.Rejected,        OccurredAt = createdAt.AddDays(3), PerformedBy = "Demo Seed", Description = "Pricing too high" },
                    new() { EventType = WorkflowEventType.QuoteModified,   OccurredAt = createdAt.AddDays(5), PerformedBy = "Demo Seed", Description = "Updated pricing based on feedback" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }

        // 5. Innovate Digital — InvoiceCreated (InvoiceFirst)
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-3), DateTimeKind.Utc);
            var invoice = new Invoice { ClientId = clients[1].Id, OrganizationId = orgId, DateCreated = createdAt, PayByDate = createdAt.AddDays(30), VatInclusive = true };
            foreach (var item in CreateInvoiceItems(38_000m, 2)) invoice.Items.Add(item);
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateInvoicePdfAsync(invoice.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[1].Id,
                Status         = WorkflowStatus.InvoiceCreated,
                Type           = WorkflowType.InvoiceFirst,
                InvoiceId      = invoice.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.InvoiceCreated, OccurredAt = createdAt, PerformedBy = "Demo Seed" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }

        // 6. Sarah van der Merwe — SentForPayment (InvoiceFirst), PayByDate future
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-14), DateTimeKind.Utc);
            var payByDate = DateTime.SpecifyKind(now.AddDays(7), DateTimeKind.Utc);
            var invoice = new Invoice { ClientId = clients[7].Id, OrganizationId = orgId, DateCreated = createdAt, PayByDate = payByDate, VatInclusive = true };
            foreach (var item in CreateInvoiceItems(75_000m, 2)) invoice.Items.Add(item);
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateInvoicePdfAsync(invoice.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[7].Id,
                Status         = WorkflowStatus.SentForPayment,
                Type           = WorkflowType.InvoiceFirst,
                InvoiceId      = invoice.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.InvoiceCreated, OccurredAt = createdAt,            PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.SentForPayment, OccurredAt = createdAt.AddDays(1), PerformedBy = "Demo Seed" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    // -------------------------------------------------------------------------
    // Scenario D — 2 Terminal Workflows
    // -------------------------------------------------------------------------

    private async Task SeedScenarioDAsync(int orgId, List<Client> clients, DateTime now, CancellationToken ct, List<Task> pdfTasks)
    {
        // 1. Innovate Digital — Cancelled
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-20), DateTimeKind.Utc);
            var quote = new Quote { ClientId = clients[1].Id, OrganizationId = orgId, DateCreated = createdAt, VatInclusive = true };
            foreach (var item in CreateQuoteItems(42_000m, 2)) quote.Items.Add(item);
            _db.Quotes.Add(quote);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[1].Id,
                Status         = WorkflowStatus.Cancelled,
                Type           = WorkflowType.QuoteFirst,
                QuoteId        = quote.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.QuoteCreated,    OccurredAt = createdAt,            PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.SentForApproval, OccurredAt = createdAt.AddDays(1), PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.Approved,        OccurredAt = createdAt.AddDays(3), PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.Cancelled,       OccurredAt = createdAt.AddDays(5), PerformedBy = "Demo Seed", Description = "Client requested cancellation" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }

        // 2. JHB Financial — Terminated
        {
            var createdAt = DateTime.SpecifyKind(now.AddDays(-15), DateTimeKind.Utc);
            var quote = new Quote { ClientId = clients[3].Id, OrganizationId = orgId, DateCreated = createdAt, VatInclusive = true };
            foreach (var item in CreateQuoteItems(28_000m, 2)) quote.Items.Add(item);
            _db.Quotes.Add(quote);
            await _db.SaveChangesAsync(ct);
            pdfTasks.Add(GenerateQuotePdfAsync(quote.Id, ct));

            _db.Workflows.Add(new Workflow
            {
                OrganizationId = orgId,
                ClientId       = clients[3].Id,
                Status         = WorkflowStatus.Terminated,
                Type           = WorkflowType.QuoteFirst,
                QuoteId        = quote.Id,
                CreatedAt      = createdAt,
                Events         = new List<WorkflowEvent>
                {
                    new() { EventType = WorkflowEventType.QuoteCreated,    OccurredAt = createdAt,            PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.SentForApproval, OccurredAt = createdAt.AddDays(1), PerformedBy = "Demo Seed" },
                    new() { EventType = WorkflowEventType.Terminated,      OccurredAt = createdAt.AddDays(3), PerformedBy = "Demo Seed", Description = "Project terminated due to budget constraints" },
                },
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    // -------------------------------------------------------------------------
    // PDF generation helpers — call PdfGeneratorService to produce PDFs for
    // seeded documents without triggering Kafka events or email notifications.
    // -------------------------------------------------------------------------

    private async Task GenerateInvoicePdfAsync(int invoiceId, CancellationToken ct)
    {
        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{pdfServiceUrl}/api/pdf/generate/invoice/{invoiceId}", null, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("PDF generation failed for Invoice {InvoiceId}: {Status}", invoiceId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not generate PDF for Invoice {InvoiceId}", invoiceId);
        }
    }

    private async Task GenerateQuotePdfAsync(int quoteId, CancellationToken ct)
    {
        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{pdfServiceUrl}/api/pdf/generate/quote/{quoteId}", null, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("PDF generation failed for Quote {QuoteId}: {Status}", quoteId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not generate PDF for Quote {QuoteId}", quoteId);
        }
    }

    // -------------------------------------------------------------------------
    // Item helpers
    // -------------------------------------------------------------------------

    private static List<InvoiceItem> CreateInvoiceItems(decimal targetAmount, int count)
    {
        var items     = new List<InvoiceItem>();
        var remaining = targetAmount;

        for (int i = 0; i < count; i++)
        {
            var desc   = ServiceDescriptions[i % ServiceDescriptions.Length];
            var isLast = i == count - 1;
            int qty    = (i % 5) + 1;

            if (isLast)
            {
                var price = Math.Round(remaining / qty, 2);
                items.Add(new InvoiceItem { Description = desc, Quantity = qty, PricePerUnit = price });
            }
            else
            {
                var share  = Math.Round(targetAmount / count, 2);
                var price  = Math.Round(share / qty, 2);
                var actual = price * qty;
                items.Add(new InvoiceItem { Description = desc, Quantity = qty, PricePerUnit = price });
                remaining -= actual;
            }
        }

        return items;
    }

    private static List<QuoteItem> CreateQuoteItems(decimal targetAmount, int count)
    {
        var items     = new List<QuoteItem>();
        var remaining = targetAmount;

        for (int i = 0; i < count; i++)
        {
            var desc   = ServiceDescriptions[i % ServiceDescriptions.Length];
            var isLast = i == count - 1;
            int qty    = (i % 5) + 1;

            if (isLast)
            {
                var price = Math.Round(remaining / qty, 2);
                items.Add(new QuoteItem { Description = desc, Quantity = qty, PricePerUnit = price });
            }
            else
            {
                var share  = Math.Round(targetAmount / count, 2);
                var price  = Math.Round(share / qty, 2);
                var actual = price * qty;
                items.Add(new QuoteItem { Description = desc, Quantity = qty, PricePerUnit = price });
                remaining -= actual;
            }
        }

        return items;
    }
}
