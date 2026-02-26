using System.Net;
using FluentAssertions;
using InvoiceTrackerApi.Tests.Helpers;

namespace InvoiceTrackerApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the health check endpoint — no auth required.
/// </summary>
public class HealthEndpointTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose() => _client.Dispose();
}
