using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.Helpers;

public static class TestDataBuilder
{
    public static Shared.Database.Models.Workflow CreateWorkflow(
        int id = 1,
        string status = "Draft",
        string type = "QuoteFirst",
        int organizationId = 1,
        int clientId = 1,
        int? quoteId = null,
        int? invoiceId = null,
        bool isActive = true)
    {
        return new Shared.Database.Models.Workflow
        {
            Id = id,
            Status = status,
            Type = type,
            OrganizationId = organizationId,
            ClientId = clientId,
            QuoteId = quoteId,
            InvoiceId = invoiceId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Events = new List<WorkflowEvent>()
        };
    }

    public static Organization CreateOrganization(
        int id = 1,
        string name = "Test Org",
        int paymentPlanId = 1)
    {
        return new Organization
        {
            Id = id,
            Name = name,
            PaymentPlanId = paymentPlanId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PaymentPlan CreatePaymentPlan(
        int id = 1,
        string name = "Basic",
        int maxUsers = 5,
        decimal monthlyCostRand = 500m)
    {
        return new PaymentPlan
        {
            Id = id,
            Name = name,
            MaxUsers = maxUsers,
            MonthlyCostRand = monthlyCostRand
        };
    }

    public static OrganizationMember CreateMember(
        int organizationId = 1,
        string userId = "user-1",
        string role = "orgUser")
    {
        return new OrganizationMember
        {
            OrganizationId = organizationId,
            UserId = userId,
            Role = role
        };
    }

    public static BankAccount CreateBankAccount(
        int id = 1,
        int organizationId = 1,
        bool active = false)
    {
        return new BankAccount
        {
            Id = id,
            OrganizationId = organizationId,
            BankName = "Test Bank",
            BranchCode = "000000",
            AccountNumber = "1234567890",
            AccountType = "Cheque",
            Active = active
        };
    }

    public static Shared.Database.Models.Quote CreateQuote(
        int id = 1,
        int clientId = 1,
        int organizationId = 1,
        List<QuoteItem>? items = null)
    {
        return new Shared.Database.Models.Quote
        {
            Id = id,
            ClientId = clientId,
            OrganizationId = organizationId,
            DateCreated = DateTime.UtcNow,
            Items = items ?? new List<QuoteItem>
            {
                new QuoteItem { Description = "Item 1", Quantity = 1, PricePerUnit = 100 }
            }
        };
    }

    public static Shared.Database.Models.Client CreateClient(int id = 1, int organizationId = 1)
    {
        return new Shared.Database.Models.Client
        {
            Id = id,
            OrganizationId = organizationId,
            Name = "Test Client",
            Email = "client@test.com",
            Cellphone = "0821234567",
            DateCreated = DateTime.UtcNow
        };
    }
}
