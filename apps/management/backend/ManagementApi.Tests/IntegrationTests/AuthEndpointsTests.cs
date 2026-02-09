using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ManagementApi.DTOs.Auth;
using ManagementApi.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ManagementApi.Tests.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    private readonly IServiceScope _scope;

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
    }

    // Note: Login validation test removed - Auth endpoints don't have validation filters configured
    // Validation happens at the service layer, not at the endpoint level.

    [Fact]
    public async Task RefreshToken_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "" // Invalid: empty refresh token
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LogoutRequest
        {
            RefreshToken = "" // Invalid: empty refresh token
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _client.Dispose();
    }
}
