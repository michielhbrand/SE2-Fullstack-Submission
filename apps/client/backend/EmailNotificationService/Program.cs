using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using EmailNotificationService.Services;
using EmailNotificationService.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5003
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5003);
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Add Background Services (Kafka consumers)
builder.Services.AddHostedService<QuoteApprovalRequestedConsumer>();
builder.Services.AddHostedService<InvoiceGeneratedConsumer>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

// Use CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "EmailNotificationService" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
