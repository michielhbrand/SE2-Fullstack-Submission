using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.BackgroundServices;
using PdfGeneratorService.Data;
using PdfGeneratorService.Services.Generation;
using PdfGeneratorService.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Configure to use port 5001
builder.WebHost.UseUrls("http://localhost:5001");

// Add detailed logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
builder.Services.AddSingleton<IMinioStorageService, MinioStorageService>();

// Add Background Services
builder.Services.AddHostedService<InvoiceCreatedConsumer>();

// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pdf-generator-service" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
