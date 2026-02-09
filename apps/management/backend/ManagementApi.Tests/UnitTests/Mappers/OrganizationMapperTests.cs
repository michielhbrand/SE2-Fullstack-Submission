using FluentAssertions;
using ManagementApi.DTOs.Organization;
using ManagementApi.Mappers;
using ManagementApi.Models;

namespace ManagementApi.Tests.UnitTests.Mappers;

public class OrganizationMapperTests
{
    [Fact]
    public void ToResponse_ValidOrganization_ShouldMapCorrectly()
    {
        // Arrange
        var organization = new Organization
        {
            Id = 1,
            Name = "Test Organization",
            TaxNumber = "TAX123",
            RegistrationNumber = "REG456",
            Email = "org@example.com",
            Phone = "+1234567890",
            Website = "https://example.com",
            Active = true,
            Address = new Address
            {
                Id = 1,
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country"
            },
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var result = organization.ToResponse();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Organization");
        result.TaxNumber.Should().Be("TAX123");
        result.RegistrationNumber.Should().Be("REG456");
        result.Email.Should().Be("org@example.com");
        result.Phone.Should().Be("+1234567890");
        result.Website.Should().Be("https://example.com");
        result.Active.Should().BeTrue();
        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be("123 Test St");
        result.Address.City.Should().Be("Test City");
        result.MemberCount.Should().Be(0);
    }

    [Fact]
    public void ToResponse_OrganizationWithNullAddress_ShouldMapCorrectly()
    {
        // Arrange
        var organization = new Organization
        {
            Id = 1,
            Name = "Test Organization",
            Active = true,
            Address = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = organization.ToResponse();

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().BeNull();
    }

    [Fact]
    public void ToResponse_Address_ShouldMapCorrectly()
    {
        // Arrange
        var address = new Address
        {
            Id = 1,
            Street = "456 Main St",
            City = "Big City",
            State = "BC",
            PostalCode = "54321",
            Country = "Country"
        };

        // Act
        var result = address.ToResponse();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Street.Should().Be("456 Main St");
        result.City.Should().Be("Big City");
        result.State.Should().Be("BC");
        result.PostalCode.Should().Be("54321");
        result.Country.Should().Be("Country");
    }

    [Fact]
    public void ToResponse_NullAddress_ShouldReturnNull()
    {
        // Arrange
        Address? address = null;

        // Act
        var result = address.ToResponse();

        // Assert
        result.Should().BeNull();
    }
}
