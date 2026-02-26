using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.Tests.Helpers;

namespace InvoiceTrackerApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the invoice API endpoints.
/// </summary>
public class InvoiceEndpointTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public InvoiceEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInvoices_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/invoice?organizationId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInvoiceById_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/invoice/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoicePdfUrl_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/invoice/99999/pdf-url");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvoice_EmptyItems_ReturnsBadRequest()
    {
        var request = new CreateInvoiceRequest
        {
            ClientId = 1,
            Items = new List<CreateInvoiceItemRequest>()   // empty items list
        };

        var response = await _client.PostAsJsonAsync("/api/invoice?organizationId=1", request);

        // BusinessRuleException → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteInvoice_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/invoice/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose() => _client.Dispose();
}
