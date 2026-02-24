using ManagementApi.Mappers;
using Shared.Database.Models;
using Xunit;

namespace ManagementApi.Tests.UnitTests.Mappers;

public class UserDirectoryMapperTests
{
    [Theory]
    [InlineData("default-roles-microservices,orgAdmin", UserRole.OrgAdmin)]
    [InlineData("orgAdmin", UserRole.OrgAdmin)]
    [InlineData("default-roles-microservices,orgUser", UserRole.OrgUser)]
    [InlineData("orgUser", UserRole.OrgUser)]
    [InlineData("default-roles-microservices,systemAdmin", UserRole.SystemAdmin)]
    [InlineData("systemAdmin", UserRole.SystemAdmin)]
    [InlineData("default-roles-microservices", UserRole.OrgUser)]
    [InlineData("", UserRole.OrgUser)]
    [InlineData(null, UserRole.OrgUser)]
    [InlineData("default-roles-microservices,orgAdmin,orgUser", UserRole.OrgAdmin)] // OrgAdmin has priority
    [InlineData("systemAdmin,orgAdmin", UserRole.SystemAdmin)] // SystemAdmin has highest priority
    [InlineData("default-roles-microservices,OrgAdmin", UserRole.OrgAdmin)] // Test uppercase variation
    [InlineData("default-roles-microservices,ORGADMIN", UserRole.OrgAdmin)] // Test all uppercase
    [InlineData("Default-Roles-Microservices,orgAdmin", UserRole.OrgAdmin)] // Test mixed case default role
    public void ToResponse_ShouldParseRoleCorrectly(string? rolesString, UserRole expectedRole)
    {
        // Arrange
        var userDirectory = new UserDirectory
        {
            Id = "test-id",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = rolesString,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = userDirectory.ToResponse();

        // Assert
        Assert.Equal(expectedRole, result.Role);
    }

    [Fact]
    public void ToResponse_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        
        var userDirectory = new UserDirectory
        {
            Id = "user-123",
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            Roles = "default-roles-microservices,orgAdmin",
            Active = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var result = userDirectory.ToResponse();

        // Assert
        Assert.Equal("user-123", result.Id);
        Assert.Equal("john.doe@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.True(result.Active);
        Assert.Equal(UserRole.OrgAdmin, result.Role);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(updatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ParseUserRole_ShouldHandleExactDatabaseScenario()
    {
        // This test mimics the exact scenario from the database:
        // Database shows: Roles = "default-roles-microservices,orgAdmin"
        // API should return: Role = OrgAdmin (not OrgUser)
        
        var userDirectory = new UserDirectory
        {
            Id = "test-user-id",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = "default-roles-microservices,orgAdmin", // Exact value from database
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = userDirectory.ToResponse();

        // Assert
        Assert.Equal(UserRole.OrgAdmin, result.Role);
        Assert.Equal("test@example.com", result.Email);
    }

    [Theory]
    [InlineData("default-roles-microservices, orgAdmin", UserRole.OrgAdmin)] // With space after comma
    [InlineData(" default-roles-microservices,orgAdmin ", UserRole.OrgAdmin)] // With leading/trailing spaces
    [InlineData("default-roles-microservices,orgAdmin ", UserRole.OrgAdmin)] // With trailing space
    [InlineData(" default-roles-microservices, orgAdmin ", UserRole.OrgAdmin)] // Multiple spaces
    public void ToResponse_ShouldHandleWhitespaceVariations(string? rolesString, UserRole expectedRole)
    {
        // Arrange
        var userDirectory = new UserDirectory
        {
            Id = "test-id",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = rolesString,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = userDirectory.ToResponse();

        // Assert
        Assert.Equal(expectedRole, result.Role);
    }
}
