using AuthApi.Extensions;
using AuthApi.Services;
using Scalar.AspNetCore;

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

// Add Kafka Producer Service
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// Add Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Add developer exception page for detailed error information
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapScalarApiReference();
}

// Global exception handler middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred while processing the request.");
        throw; // Re-throw to let the default exception handler deal with it
    }
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Health endpoint
app.MapHealthChecks("/health");

app.Run();
