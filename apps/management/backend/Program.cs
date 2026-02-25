using FluentValidation;
using ManagementApi.Endpoints.Auth;
using ManagementApi.Endpoints.Organization;
using ManagementApi.Endpoints.PaymentPlan;
using ManagementApi.Endpoints.User;
using ManagementApi.Exceptions;
using ManagementApi.Extensions;
using ManagementApi.Services.Auth;
using ManagementApi.Services.User;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5002");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Use PascalCase (default C# naming)
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IKeycloakAuthService, KeycloakAuthService>();
builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseExceptionHandler();
app.UseCors("AllowManagementPortal");

// Add rate limiting middleware before authentication
app.UseRateLimitingMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapPaymentPlanEndpoints();
app.MapUserEndpoints();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
