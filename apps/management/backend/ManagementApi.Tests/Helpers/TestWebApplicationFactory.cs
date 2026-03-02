using Shared.Database.Data;
using ManagementApi.Services.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ManagementApi.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Evaluated once per factory instance so all DbContext providers share the same in-memory store
    private readonly string _dbName = $"TestDatabase_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextServiceDescriptor != null)
            {
                services.Remove(dbContextServiceDescriptor);
            }

            // Add in-memory database — capture the name in a closure so the seeding
            // provider and the app's provider both resolve the exact same in-memory store.
            var dbName = _dbName;
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Mock Keycloak services for integration tests
            services.RemoveAll(typeof(IKeycloakTokenService));
            services.RemoveAll(typeof(IKeycloakUserAdminService));
            services.RemoveAll(typeof(IKeycloakRoleService));

            services.AddSingleton(new Mock<IKeycloakTokenService>().Object);
            services.AddSingleton(new Mock<IKeycloakUserAdminService>().Object);
            services.AddSingleton(new Mock<IKeycloakRoleService>().Object);

            // Disable authorization for integration tests
            services.AddAuthorization(options =>
            {
                // Allow all requests to pass authorization
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build();
                
                // Override specific policies
                options.AddPolicy("SystemAdminOnly", policy => policy.RequireAssertion(_ => true));
                
                // Override role-based authorization
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build();
            });

            // Disable authentication requirement
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<TestWebApplicationFactory>>();

            // Ensure the database is created
            db.Database.EnsureCreated();

            try
            {
                // Seed test data if needed
                SeedTestData(db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the database with test data.");
            }
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        // Seed the three payment plans so the CreateOrganization endpoint can resolve the default (Basic, Id=1)
        if (!context.PaymentPlans.Any())
        {
            context.PaymentPlans.AddRange(
                new Shared.Database.Models.PaymentPlan { Id = 1, Name = "Basic",    MaxUsers = 5,  MonthlyCostRand = 500m  },
                new Shared.Database.Models.PaymentPlan { Id = 2, Name = "Advanced", MaxUsers = 15, MonthlyCostRand = 2500m },
                new Shared.Database.Models.PaymentPlan { Id = 3, Name = "Ultimate", MaxUsers = -1, MonthlyCostRand = 4000m }
            );
            context.SaveChanges();
        }
    }
}
