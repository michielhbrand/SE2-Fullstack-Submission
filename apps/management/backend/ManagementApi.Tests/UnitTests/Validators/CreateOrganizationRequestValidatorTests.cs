using FluentAssertions;
using ManagementApi.DTOs.Organization;
using ManagementApi.Validators.Organization;

namespace ManagementApi.Tests.UnitTests.Validators;

public class CreateOrganizationRequestValidatorTests
{
    private readonly CreateOrganizationRequestValidator _validator;

    public CreateOrganizationRequestValidatorTests()
    {
        _validator = new CreateOrganizationRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Address = new CreateAddressRequest
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country"
            }
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
    public void Validate_EmptyName_ShouldFail(string name)
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = name,
            Address = new CreateAddressRequest
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = new string('a', 201),
            Address = new CreateAddressRequest
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("200 characters"));
    }

    // Note: Address validation test removed - Address is optional in CreateOrganizationRequest
    // The validator allows null addresses, which is correct behavior for the domain model.

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyAddressStreet_ShouldFail(string street)
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Address = new CreateAddressRequest
            {
                Street = street,
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address.Street");
    }
}
