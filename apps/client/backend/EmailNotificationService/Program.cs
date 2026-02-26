using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using EmailNotificationService.Services;
using EmailNotificationService.BackgroundServices;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5003
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5003);
});

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithSpan()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("EmailNotificationService"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSource("QuoteApprovalRequestedConsumer")
        .AddSource("InvoiceGeneratedConsumer")
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")));

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
