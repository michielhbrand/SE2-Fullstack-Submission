using InvoiceTrackerApi.Extensions;
using InvoiceTrackerApi.Middleware;
using InvoiceTrackerApi.Services;
using InvoiceTrackerApi.Services.Auth;
using InvoiceTrackerApi.Services.Client;
using InvoiceTrackerApi.Services.Invoice;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceTrackerApi.Services.Quote;
using InvoiceTrackerApi.Services.Template;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Invoice;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Repositories.Template;

var builder = WebApplication.CreateBuilder(args);

// Configure to always use port 5000
builder.WebHost.UseUrls("http://localhost:5000");

// Add detailed logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services using extension methods
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddHealthCheckServices();

// Register Repositories (Data Access Layer)
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();

// Register Services (Business Logic Layer)
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IPdfStorageService, PdfStorageService>();

// Add Kafka Producer Service
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// Add Keycloak Authentication Service
builder.Services.AddScoped<IKeycloakAuthService, KeycloakAuthService>();

// Add HttpClient
builder.Services.AddHttpClient();

// Add Middleware
builder.Services.AddTransient<GlobalExceptionMiddleware>();

// Add Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Add developer exception page for detailed error information
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Use NSwag OpenAPI middleware
    app.UseOpenApi(); // Serves the OpenAPI/Swagger document at /swagger/v1/swagger.json
    app.UseSwaggerUi(); // Serves the Swagger UI at /swagger
}

// Global exception handler middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Health endpoint
app.MapHealthChecks("/health");

app.Run();
