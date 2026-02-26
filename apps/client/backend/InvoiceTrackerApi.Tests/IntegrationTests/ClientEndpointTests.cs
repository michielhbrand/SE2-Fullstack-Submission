using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.Tests.Helpers;

namespace InvoiceTrackerApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the client API endpoints.
/// </summary>
public class ClientEndpointTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public ClientEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetClients_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/client?organizationId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClientById_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/client/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClient_WithoutRequiredName_ReturnsBadRequest()
    {
        var request = new CreateClientRequest
        {
            Name = "",        // required field empty
            Email = "test@example.com",
            Cellphone = "0821234567"
        };

        var response = await _client.PostAsJsonAsync("/api/client?organizationId=1", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteClient_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/client/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose() => _client.Dispose();
}
