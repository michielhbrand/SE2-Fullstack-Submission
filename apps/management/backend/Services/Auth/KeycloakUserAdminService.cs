using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Shared.Core.Keycloak.Models;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using Shared.Database.Models;

namespace ManagementApi.Services.Auth;

public class KeycloakUserAdminService : IKeycloakUserAdminService
{
    private readonly HttpClient _httpClient;
    private readonly IKeycloakTokenService _tokenService;
    private readonly IKeycloakRoleService _roleService;
    private readonly ILogger<KeycloakUserAdminService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;

    public KeycloakUserAdminService(
        HttpClient httpClient,
        IOptions<KeycloakOptions> options,
        IKeycloakTokenService tokenService,
        IKeycloakRoleService roleService,
        ILogger<KeycloakUserAdminService> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _roleService = roleService;
        _logger = logger;

        var authorityUri = new Uri(options.Value.Authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _keycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        _realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";
    }

    public async Task<KeycloakUserResponse> CreateUserAsync(string email, string? firstName, string? lastName, string password, UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var usersEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users";

            var roleString = role.ToString();
            roleString = char.ToLowerInvariant(roleString[0]) + roleString.Substring(1);

            var userPayload = new
            {
                username = email,
                email = email,
                firstName = firstName ?? string.Empty,
                lastName = lastName ?? string.Empty,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new { type = "password", value = password, temporary = false }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, usersEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = jsonContent;
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create user. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    throw new ValidationException("A user with this email already exists");

                throw new ServiceUnavailableException("Failed to create user in Keycloak");
            }

            var locationHeader = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(locationHeader))
                throw new ServiceUnavailableException("Failed to retrieve created user ID");

            var userId = locationHeader.Split('/').Last();

            _logger.LogInformation("User created with ID: {UserId}, now assigning role: {Role}", userId, roleString);

            await _roleService.AssignRoleToUserAsync(userId, roleString, cancellationToken);

            return await GetUserByIdAsync(userId, cancellationToken);
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in Keycloak");
            throw new ServiceUnavailableException("Failed to create user");
        }
    }

    public async Task<KeycloakUserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var usersEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true";

            using var request = new HttpRequestMessage(HttpMethod.Get, usersEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get user by email. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to retrieve user from Keycloak");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var users = JsonSerializer.Deserialize<List<KeycloakUserResponse>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users == null || users.Count == 0)
                throw new NotFoundException($"User with email '{email}' not found");

            return users[0];
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email from Keycloak");
            throw new ServiceUnavailableException("Failed to retrieve user");
        }
    }

    public async Task<KeycloakUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var userEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}";

            using var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new NotFoundException($"User with ID '{userId}' not found");

                _logger.LogError("Failed to get user by ID. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to retrieve user from Keycloak");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var user = JsonSerializer.Deserialize<KeycloakUserResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (user == null)
                throw new NotFoundException($"User with ID '{userId}' not found");

            return user;
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID from Keycloak");
            throw new ServiceUnavailableException("Failed to retrieve user");
        }
    }

    public async Task UpdateUserAsync(string userId, string? firstName, string? lastName, bool? enabled, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var userEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}";

            var currentUser = await GetUserByIdAsync(userId, cancellationToken);

            var updatePayload = new
            {
                firstName = firstName ?? currentUser.FirstName ?? string.Empty,
                lastName = lastName ?? currentUser.LastName ?? string.Empty,
                enabled = enabled ?? currentUser.Enabled
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, userEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = jsonContent;
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to update user. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new NotFoundException($"User with ID '{userId}' not found");

                throw new ServiceUnavailableException("Failed to update user in Keycloak");
            }

            _logger.LogInformation("Successfully updated user {UserId}", userId);
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user in Keycloak");
            throw new ServiceUnavailableException("Failed to update user");
        }
    }
}
