using ManagementApi.Models;

namespace ManagementApi.Tests.Helpers;

public static class TestDataBuilder
{
    public static Organization CreateTestOrganization(
        int id = 1,
        string name = "Test Organization",
        bool active = true)
    {
        return new Organization
        {
            Id = id,
            Name = name,
            Active = active,
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "Test Country"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static User CreateTestUser(
        string id = "test-user-id",
        bool active = true)
    {
        return new User
        {
            Id = id,
            Active = active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static UserDirectory CreateTestUserDirectory(
        string id = "test-user-id",
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        bool active = true)
    {
        return new UserDirectory
        {
            Id = id,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Active = active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static OrganizationMember CreateTestOrganizationMember(
        int organizationId = 1,
        string userId = "test-user-id")
    {
        return new OrganizationMember
        {
            OrganizationId = organizationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
    }
}
