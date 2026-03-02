using FluentValidation;
using ManagementApi.Endpoints.Auth;
using ManagementApi.Endpoints.Organization;
using ManagementApi.Endpoints.PaymentPlan;
using ManagementApi.Endpoints.User;
using ManagementApi.Extensions;
using Shared.Core.Exceptions;
using Shared.Core.Extensions;
using ManagementApi.Services.Auth;
using ManagementApi.Services.SeedData;
using ManagementApi.Services.User;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:5002");

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithSpan()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("ManagementApi"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")));

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddCorsPolicy("AllowManagementPortal", "http://localhost:5174");
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireRole("systemAdmin"));
});
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
builder.Services.AddScoped<ISeedDemoDataService, SeedDemoDataService>();

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
