using FluentAssertions;
using Shared.Core.Keycloak.Models;
using ManagementApi.Mappers;
using Shared.Database.Models;

namespace ManagementApi.Tests.UnitTests.Mappers;

public class UserMapperTests
{
    [Fact]
    public void ToResponseAsync_ValidUserAndKeycloakUser_ShouldMapCorrectly()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new User
        {
            Id = userId,
            Active = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var keycloakUser = new KeycloakUserResponse
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Enabled = true
        };

        // Act
        var result = user.ToResponseAsync(keycloakUser);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
        result.Active.Should().BeTrue();
        result.CreatedAt.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.UpdatedAt.Should().Be(new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToResponseAsync_InactiveUser_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = "inactive-user",
            Active = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var keycloakUser = new KeycloakUserResponse
        {
            Id = "inactive-user",
            Email = "inactive@example.com",
            FirstName = "Inactive",
            LastName = "User",
            Enabled = false
        };

        // Act
        var result = user.ToResponseAsync(keycloakUser);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public void ToResponseAsync_NullNames_ShouldMapCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = "test-user",
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var keycloakUser = new KeycloakUserResponse
        {
            Id = "test-user",
            Email = "test@example.com",
            FirstName = null,
            LastName = null,
            Enabled = true
        };

        // Act
        var result = user.ToResponseAsync(keycloakUser);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
    }
}
