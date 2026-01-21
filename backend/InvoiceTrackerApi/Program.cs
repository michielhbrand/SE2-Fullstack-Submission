using InvoiceTrackerApi.Extensions;
using InvoiceTrackerApi.Exceptions;
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

builder.WebHost.UseUrls("http://localhost:5000");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddHealthCheckServices();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IPdfStorageService, PdfStorageService>();

builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddScoped<IKeycloakAuthService, KeycloakAuthService>();

builder.Services.AddHttpClient();

builder.Services.AddControllers();

var app = builder.Build();

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
