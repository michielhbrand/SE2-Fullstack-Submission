using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Workflow.Responses;
using InvoiceTrackerApi.Tests.Helpers;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the workflow API endpoints.
/// </summary>
public class WorkflowEndpointTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public WorkflowEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWorkflows_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/workflow?organizationId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedWorkflowResponse>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkflowById_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/workflow/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkflow_InvoiceFirst_WithoutInvoiceId_ReturnsBadRequest()
    {
        // InvoiceFirst without InvoiceId → BusinessRuleException → 400
        var request = new CreateWorkflowRequest
        {
            Type = WorkflowType.InvoiceFirst,
            ClientId = 1,
            InvoiceId = null
        };

        var response = await _client.PostAsJsonAsync("/api/workflow?organizationId=1", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWorkflow_MissingQuoteId_ReturnsBadRequest()
    {
        // QuoteFirst workflow without a QuoteId is a BusinessRuleException (400)
        var request = new CreateWorkflowRequest
        {
            Type = WorkflowType.QuoteFirst,
            ClientId = 1,
            QuoteId = null
        };

        var response = await _client.PostAsJsonAsync("/api/workflow?organizationId=1", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWorkflowsBySearch_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/workflow?organizationId=1&search=client");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Helper class for deserializing paginated workflow response
    private class PaginatedWorkflowResponse
    {
        public List<WorkflowListItemResponse>? Data { get; set; }
    }

    public void Dispose() => _client.Dispose();
}
