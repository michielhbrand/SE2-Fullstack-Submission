using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.BackgroundServices;
using PdfGeneratorService.Data;
using PdfGeneratorService.Services.Generation;
using PdfGeneratorService.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001);
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
builder.Services.AddScoped<IMinioStorageService, MinioStorageService>();

// Add Background Services
builder.Services.AddHostedService<InvoiceCreatedConsumer>();
builder.Services.AddHostedService<MinioInitializationService>();

// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "PdfGeneratorService" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
