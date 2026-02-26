using FluentAssertions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Tests.Helpers;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.UnitTests.Mappers;

public class OrganizationMappingTests
{
    [Fact]
    public void Organization_ToDto_MapsBasicFields()
    {
        var org = TestDataBuilder.CreateOrganization(id: 42, name: "Acme Corp");
        org.VatRate = 15m;

        var dto = org.ToDto();

        dto.Id.Should().Be(42);
        dto.Name.Should().Be("Acme Corp");
        dto.VatRate.Should().Be(15m);
    }

    [Fact]
    public void Organization_ToDto_IncludesPaymentPlanFields()
    {
        var org = TestDataBuilder.CreateOrganization();
        org.PaymentPlan = TestDataBuilder.CreatePaymentPlan(id: 2, name: "Advanced", maxUsers: 15);

        var dto = org.ToDto();

        dto.PaymentPlanName.Should().Be("Advanced");
        dto.PaymentPlanMaxUsers.Should().Be(15);
    }

    [Fact]
    public void Organization_ToDto_WithNullPaymentPlan_HasNullPlanFields()
    {
        var org = TestDataBuilder.CreateOrganization();
        org.PaymentPlan = null;

        var dto = org.ToDto();

        dto.PaymentPlanName.Should().BeNull();
        dto.PaymentPlanMaxUsers.Should().BeNull();
    }

    [Fact]
    public void BankAccount_ToDto_MapsAllFields()
    {
        var account = new BankAccount
        {
            Id = 7,
            BankName = "FNB",
            BranchCode = "250655",
            AccountNumber = "62123456789",
            AccountType = "Cheque",
            Active = true,
            OrganizationId = 1
        };

        var dto = account.ToDto();

        dto.Id.Should().Be(7);
        dto.BankName.Should().Be("FNB");
        dto.BranchCode.Should().Be("250655");
        dto.AccountNumber.Should().Be("62123456789");
        dto.AccountType.Should().Be("Cheque");
        dto.Active.Should().BeTrue();
    }
}
