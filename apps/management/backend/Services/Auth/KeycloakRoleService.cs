using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ManagementApi.DTOs.Auth;
using Microsoft.Extensions.Options;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using Shared.Database.Models;

namespace ManagementApi.Services.Auth;

public class KeycloakRoleService : IKeycloakRoleService
{
    private readonly HttpClient _httpClient;
    private readonly IKeycloakTokenService _tokenService;
    private readonly ILogger<KeycloakRoleService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;

    public KeycloakRoleService(
        HttpClient httpClient,
        IOptions<KeycloakOptions> options,
        IKeycloakTokenService tokenService,
        ILogger<KeycloakRoleService> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;

        var authorityUri = new Uri(options.Value.Authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _keycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        _realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";
    }

    public async Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var roleMappingsEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";

            using var request = new HttpRequestMessage(HttpMethod.Get, roleMappingsEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User {UserId} not found when fetching roles", userId);
                    return new List<string>();
                }

                _logger.LogError("Failed to get user roles. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to retrieve user roles from Keycloak");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var roles = JsonSerializer.Deserialize<List<KeycloakRoleResponse>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return roles?.Select(r => r.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles from Keycloak for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task UpdateUserRoleAsync(string userId, UserRole newRole, CancellationToken cancellationToken = default)
    {
        try
        {
            var newRoleString = newRole.ToString();
            newRoleString = char.ToLowerInvariant(newRoleString[0]) + newRoleString.Substring(1);

            _logger.LogInformation("Updating user {UserId} role to {Role}", userId, newRoleString);

            var currentRoles = await GetUserRolesAsync(userId, cancellationToken);
            var rolesToRemove = currentRoles.Where(r => r == "orgUser" || r == "orgAdmin").ToList();

            if (rolesToRemove.Any())
                await RemoveRolesFromUserAsync(userId, rolesToRemove, cancellationToken);

            await AssignRoleToUserAsync(userId, newRoleString, cancellationToken);

            _logger.LogInformation("Successfully updated user {UserId} role to {Role}", userId, newRoleString);
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role in Keycloak for user {UserId}", userId);
            throw new ServiceUnavailableException("Failed to update user role");
        }
    }

    public async Task AssignRoleToUserAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var rolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/roles/{roleName}";

            using var roleRequest = new HttpRequestMessage(HttpMethod.Get, rolesEndpoint);
            roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var roleResponse = await _httpClient.SendAsync(roleRequest, cancellationToken);

            if (!roleResponse.IsSuccessStatusCode)
            {
                var errorContent = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get role {Role}. Status: {Status}, Error: {Error}", roleName, roleResponse.StatusCode, errorContent);
                throw new ServiceUnavailableException($"Failed to retrieve role '{roleName}' from Keycloak");
            }

            var roleContent = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
            var roleData = JsonSerializer.Deserialize<JsonElement>(roleContent);
            var roleId = roleData.GetProperty("id").GetString();
            var roleNameFromKeycloak = roleData.GetProperty("name").GetString();

            var roleAssignment = new[] { new { id = roleId, name = roleNameFromKeycloak } };
            var assignContent = new StringContent(JsonSerializer.Serialize(roleAssignment), Encoding.UTF8, "application/json");

            using var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm");
            assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            assignRequest.Content = assignContent;
            var assignResponse = await _httpClient.SendAsync(assignRequest, cancellationToken);

            if (!assignResponse.IsSuccessStatusCode)
            {
                var errorContent = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to assign role {Role} to user {UserId}. Status: {Status}, Error: {Error}", roleName, userId, assignResponse.StatusCode, errorContent);
                throw new ServiceUnavailableException($"Failed to assign role '{roleName}' to user");
            }

            _logger.LogInformation("Successfully assigned role {Role} to user {UserId}", roleName, userId);
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", roleName, userId);
            throw new ServiceUnavailableException("Failed to assign role to user");
        }
    }

    public async Task RemoveRolesFromUserAsync(string userId, List<string> roleNames, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
            var rolesToRemove = new List<object>();

            foreach (var roleName in roleNames)
            {
                using var roleRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakUrl}/admin/realms/{_realm}/roles/{roleName}");
                roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                var roleResponse = await _httpClient.SendAsync(roleRequest, cancellationToken);

                if (roleResponse.IsSuccessStatusCode)
                {
                    var roleContent = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                    var roleData = JsonSerializer.Deserialize<JsonElement>(roleContent);
                    rolesToRemove.Add(new
                    {
                        id = roleData.GetProperty("id").GetString(),
                        name = roleData.GetProperty("name").GetString()
                    });
                }
            }

            if (!rolesToRemove.Any()) return;

            var userRoleMappingEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
            var jsonContent = new StringContent(JsonSerializer.Serialize(rolesToRemove), Encoding.UTF8, "application/json");

            using var removeRequest = new HttpRequestMessage(HttpMethod.Delete, userRoleMappingEndpoint);
            removeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            removeRequest.Content = jsonContent;
            var removeResponse = await _httpClient.SendAsync(removeRequest, cancellationToken);

            if (!removeResponse.IsSuccessStatusCode)
            {
                var errorContent = await removeResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to remove roles from user {UserId}. Status: {Status}, Error: {Error}", userId, removeResponse.StatusCode, errorContent);
                throw new ServiceUnavailableException("Failed to remove existing roles from user");
            }

            _logger.LogInformation("Successfully removed roles from user {UserId}", userId);
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing roles from user {UserId}", userId);
            throw new ServiceUnavailableException("Failed to remove roles from user");
        }
    }
}
