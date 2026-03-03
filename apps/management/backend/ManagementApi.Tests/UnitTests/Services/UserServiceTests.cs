using FluentAssertions;
using Shared.Database.Data;
using ManagementApi.DTOs.User;
using Shared.Core.Exceptions.Application;
using Shared.Core.Keycloak.Models;
using Shared.Database.Models;
using ManagementApi.Services.Auth;
using ManagementApi.Services.User;
using ManagementApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ManagementApi.Tests.UnitTests.Services;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IKeycloakUserAdminService> _keycloakUserAdminMock;
    private readonly Mock<IKeycloakRoleService> _keycloakRoleMock;
    private readonly Mock<IUserDirectoryService> _userDirectoryServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _keycloakUserAdminMock = new Mock<IKeycloakUserAdminService>();
        _keycloakRoleMock = new Mock<IKeycloakRoleService>();
        _userDirectoryServiceMock = new Mock<IUserDirectoryService>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _context,
            _keycloakUserAdminMock.Object,
            _keycloakRoleMock.Object,
            _userDirectoryServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_ShouldCreateUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        var keycloakUser = new KeycloakUserResponse
        {
            Id = "keycloak-user-id",
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Enabled = true
        };

        var userResponse = new UserResponse
        {
            Id = keycloakUser.Id,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Active = request.Active,
            CreatedAt = DateTime.UtcNow
        };

        _keycloakUserAdminMock
            .Setup(x => x.CreateUserAsync(
                request.Email,
                request.FirstName,
                request.LastName,
                It.IsAny<string>(),
                request.Role,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(keycloakUser);

        _userDirectoryServiceMock
            .Setup(x => x.SyncUserAsync(keycloakUser.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userDirectoryServiceMock
            .Setup(x => x.GetUserByIdAsync(keycloakUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userResponse);

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Id == keycloakUser.Id);
        userInDb.Should().NotBeNull();
        userInDb!.Active.Should().Be(request.Active);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ShouldThrowValidationException()
    {
        // Arrange
        var existingUser = TestDataBuilder.CreateTestUser("existing-user-id");
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Email = "existing-user-id", // Using ID as email since that's how it's checked
            FirstName = "Test",
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _userService.CreateUserAsync(request));
    }

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_ShouldUpdateUser()
    {
        // Arrange
        var userId = "test-user-id";
        var user = TestDataBuilder.CreateTestUser(userId);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Active = false
        };

        var updatedResponse = new UserResponse
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Updated",
            LastName = "Name",
            Active = false,
            CreatedAt = user.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _keycloakUserAdminMock
            .Setup(x => x.UpdateUserAsync(
                userId,
                request.FirstName,
                request.LastName,
                request.Active,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userDirectoryServiceMock
            .Setup(x => x.SyncUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userDirectoryServiceMock
            .Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedResponse);

        // Act
        var result = await _userService.UpdateUserAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("Name");
        result.Active.Should().BeFalse();

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        userInDb.Should().NotBeNull();
        userInDb!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.UpdateUserAsync("non-existent-id", request));
    }

    [Fact]
    public async Task RemoveUserFromOrganizationAsync_ValidRequest_ShouldRemoveMembership()
    {
        // Arrange
        var organization = TestDataBuilder.CreateTestOrganization(1);
        var user = TestDataBuilder.CreateTestUser("user-id");
        var membership = TestDataBuilder.CreateTestOrganizationMember(1, "user-id");

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        await _userService.RemoveUserFromOrganizationAsync(1, "user-id");

        // Assert
        var membershipInDb = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == 1 && m.UserId == "user-id");
        membershipInDb.Should().BeNull();
    }

    [Fact]
    public async Task RemoveUserFromOrganizationAsync_NonExistentMembership_ShouldThrowNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.RemoveUserFromOrganizationAsync(1, "non-existent-user"));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
