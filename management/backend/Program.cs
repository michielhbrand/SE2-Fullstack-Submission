using FluentValidation;
using ManagementApi.Endpoints.Auth;
using ManagementApi.Endpoints.Organization;
using ManagementApi.Endpoints.User;
using ManagementApi.Exceptions;
using ManagementApi.Extensions;
using ManagementApi.Services.Auth;
using ManagementApi.Services.User;

var builder = WebApplication.CreateBuilder(args);

// Configure URL
builder.WebHost.UseUrls("http://localhost:5002");

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);

// Configure JSON serialization to use PascalCase (matching C# DTOs)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Use PascalCase (default C# naming)
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IKeycloakAuthService, KeycloakAuthService>();
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
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map API endpoints
app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapUserEndpoints();

app.Run();
