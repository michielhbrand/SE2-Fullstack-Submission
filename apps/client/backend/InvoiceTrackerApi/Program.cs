using InvoiceTrackerApi.Extensions;
using Shared.Core.Exceptions;
using Shared.Core.Extensions;
using InvoiceTrackerApi.Services;
using InvoiceTrackerApi.Services.Auth;
using InvoiceTrackerApi.Services.Client;
using InvoiceTrackerApi.Services.Invoice;
using InvoiceTrackerApi.Services.Organization;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceTrackerApi.Services.Quote;
using InvoiceTrackerApi.Services.Template;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Invoice;
using InvoiceTrackerApi.Repositories.Organization;
using InvoiceTrackerApi.Repositories.OrganizationMember;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Repositories.Template;
using InvoiceTrackerApi.Services.User;
using InvoiceTrackerApi.Services.Workflow;
using InvoiceTrackerApi.Services.Dashboard;
using InvoiceTrackerApi.Repositories.User;
using InvoiceTrackerApi.Repositories.Workflow;
using InvoiceTrackerApi.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:5000");

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithSpan()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("InvoiceTrackerApi"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")));

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddCorsPolicy("AllowFrontend", "http://localhost:5173", "http://localhost:3000");
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddHealthCheckServices();
builder.Services.AddValidationServices();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IPdfStorageService, PdfStorageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuoteToInvoiceConversionService, QuoteToInvoiceConversionService>();
builder.Services.AddScoped<IWorkflowEventDispatcher, WorkflowEventDispatcher>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<OverdueInvoiceCheckService>();
builder.Services.AddHostedService<DefaultTemplateSeedService>();
builder.Services.AddScoped<IKeycloakAuthService, KeycloakAuthService>();
builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();

builder.Services.AddHttpClient();

builder.Services.AddControllers(options =>
    options.Filters.Add<InvoiceTrackerApi.Filters.OrganizationAuthorizationFilter>());

var app = builder.Build();

// Apply any pending EF Core migrations on startup so Docker users need no manual steps.
// Guard against non-relational providers (e.g. InMemory used in integration tests).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    app.UseOpenApi(); 
    app.UseSwaggerUi(); 
}
    
app.UseExceptionHandler();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
