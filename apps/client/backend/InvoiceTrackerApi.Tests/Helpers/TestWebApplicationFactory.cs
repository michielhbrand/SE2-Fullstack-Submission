using InvoiceTrackerApi.Services;
using InvoiceTrackerApi.Services.Auth;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceTrackerApi.Services.Workflow;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Database.Data;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Stable name per factory instance so the seeding provider and the app provider
    // both resolve the exact same in-memory store.
    private readonly string _dbName = $"InvoiceTrackerTest_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── Replace real DbContext with in-memory ──────────────────────────
            var existingDb = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (existingDb != null) services.Remove(existingDb);

            var existingDbService = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (existingDbService != null) services.Remove(existingDbService);

            var dbName = _dbName;
            services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase(dbName));

            // ── Mock Kafka (singleton — must be removed before replacing) ──────
            var kafkaDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IKafkaProducerService));
            if (kafkaDescriptor != null) services.Remove(kafkaDescriptor);
            services.AddSingleton(_ => new Mock<IKafkaProducerService>().Object);

            // ── Mock PDF storage service ───────────────────────────────────────
            var pdfDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPdfStorageService));
            if (pdfDescriptor != null) services.Remove(pdfDescriptor);
            var pdfMock = new Mock<IPdfStorageService>();
            pdfMock.Setup(s => s.GetPresignedUrlAsync(It.IsAny<string>()))
                   .ReturnsAsync("https://storage.example.com/test.pdf");
            services.AddScoped(_ => pdfMock.Object);

            // ── Mock Keycloak auth service ─────────────────────────────────────
            var keycloakDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IKeycloakAuthService));
            if (keycloakDescriptor != null) services.Remove(keycloakDescriptor);
            services.AddScoped(_ => new Mock<IKeycloakAuthService>().Object);

            // ── Break circular DI: InvoiceService → IWorkflowService → IQuoteToInvoiceConversionService → IInvoiceService ──
            services.RemoveAll(typeof(IQuoteToInvoiceConversionService));
            services.AddScoped(_ => new Mock<IQuoteToInvoiceConversionService>().Object);

            // ── Bypass all authorization ───────────────────────────────────────
            services.AddAuthorization(opts =>
            {
                opts.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true).Build();
                opts.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true).Build();
            });

            // ── Test authentication scheme ─────────────────────────────────────
            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = "Test";
                opts.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // ── Seed required reference data ───────────────────────────────────
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestWebApplicationFactory>>();
            db.Database.EnsureCreated();

            try { SeedTestData(db); }
            catch (Exception ex) { logger.LogError(ex, "Error seeding integration test data"); }
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        if (!context.PaymentPlans.Any())
        {
            context.PaymentPlans.AddRange(
                new PaymentPlan { Id = 1, Name = "Basic",    MaxUsers = 5,  MonthlyCostRand = 500m  },
                new PaymentPlan { Id = 2, Name = "Advanced", MaxUsers = 15, MonthlyCostRand = 2500m },
                new PaymentPlan { Id = 3, Name = "Ultimate", MaxUsers = -1, MonthlyCostRand = 4000m }
            );
            context.SaveChanges();
        }

        if (!context.Organizations.Any())
        {
            context.Organizations.Add(new Organization
            {
                Id = 1,
                Name = "Test Organisation",
                Active = true,
                PaymentPlanId = 1
            });
            context.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = 1,
                UserId = TestAuthHandler.TestUserId,
                Role = "orgUser"
            });
            context.SaveChanges();
        }
    }
}
