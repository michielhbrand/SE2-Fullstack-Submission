using FluentAssertions;
using ManagementApi.DTOs.User;
using Shared.Database.Models;
using ManagementApi.Validators.User;

namespace ManagementApi.Tests.UnitTests.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator;

    public CreateUserRequestValidatorTests()
    {
        _validator = new CreateUserRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_EmptyEmail_ShouldFail(string email)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void Validate_InvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("Invalid email format"));
    }

    [Fact]
    public void Validate_EmailTooLong_ShouldFail()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com"; // 263 characters
        var request = new CreateUserRequest
        {
            Email = longEmail,
            FirstName = "Test",
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public void Validate_FirstNameTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = new string('a', 101),
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public void Validate_LastNameTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = new string('a', 101),
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public void Validate_InvalidRole_ShouldFail()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Active = true,
            Role = (UserRole)999 // Invalid enum value
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role" && e.ErrorMessage.Contains("valid UserRole"));
    }

    [Fact]
    public void Validate_NullFirstName_ShouldPass()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = null,
            LastName = "User",
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullLastName_ShouldPass()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = null,
            Active = true,
            Role = UserRole.OrgUser
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
