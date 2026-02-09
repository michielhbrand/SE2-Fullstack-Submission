using FluentAssertions;
using ManagementApi.DTOs.Auth;
using ManagementApi.Validators.Auth;

namespace ManagementApi.Tests.UnitTests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "testpassword"
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
    public void Validate_EmptyUsername_ShouldFail(string username)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = username,
            Password = "testpassword"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_EmptyPassword_ShouldFail(string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = password
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_BothFieldsEmpty_ShouldFailWithMultipleErrors()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
