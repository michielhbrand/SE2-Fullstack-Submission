using FluentAssertions;
using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Repositories.Organization;
using InvoiceTrackerApi.Repositories.OrganizationMember;
using InvoiceTrackerApi.Services.Organization;
using InvoiceTrackerApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Database.Data;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.UnitTests.Services;

public class OrganizationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IOrganizationRepository> _orgRepoMock;
    private readonly Mock<IOrganizationMemberRepository> _memberRepoMock;
    private readonly Mock<ILogger<OrganizationService>> _loggerMock;
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _orgRepoMock = new Mock<IOrganizationRepository>();
        _memberRepoMock = new Mock<IOrganizationMemberRepository>();
        _loggerMock = new Mock<ILogger<OrganizationService>>();

        _service = new OrganizationService(
            _orgRepoMock.Object,
            _memberRepoMock.Object,
            _context,
            _loggerMock.Object);
    }

    // ─── AddMember ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMember_NonAdmin_ThrowsForbiddenException()
    {
        var org = TestDataBuilder.CreateOrganization();
        _orgRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(org);
        _memberRepoMock.Setup(r => r.HasRoleAsync(1, "requester", "orgAdmin")).ReturnsAsync(false);

        var act = () => _service.AddMemberToOrganizationAsync(1, "new-user",
            new AddOrganizationMemberRequest { Role = "orgUser" }, "requester");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddMember_AlreadyMember_ThrowsConflictException()
    {
        var org = TestDataBuilder.CreateOrganization();
        _orgRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(org);
        _memberRepoMock.Setup(r => r.HasRoleAsync(1, "admin", "orgAdmin")).ReturnsAsync(true);
        _memberRepoMock.Setup(r => r.GetMembershipAsync(1, "existing-user"))
            .ReturnsAsync(TestDataBuilder.CreateMember(1, "existing-user"));

        var act = () => _service.AddMemberToOrganizationAsync(1, "existing-user",
            new AddOrganizationMemberRequest { Role = "orgUser" }, "admin");

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AddMember_BasicPlanAtCapacity_ThrowsBusinessRuleException()
    {
        // Basic plan: 5 users max. Seed 5 existing members and try to add a 6th.
        var plan = TestDataBuilder.CreatePaymentPlan(id: 1, maxUsers: 5);
        await _context.PaymentPlans.AddAsync(plan);

        var org = TestDataBuilder.CreateOrganization(paymentPlanId: 1);
        await _context.SaveChangesAsync();

        _orgRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(org);
        _memberRepoMock.Setup(r => r.HasRoleAsync(1, "admin", "orgAdmin")).ReturnsAsync(true);
        _memberRepoMock.Setup(r => r.GetMembershipAsync(1, "new-user")).ReturnsAsync((OrganizationMember?)null);

        // 5 existing members
        var existingMembers = Enumerable.Range(1, 5)
            .Select(i => TestDataBuilder.CreateMember(1, $"user-{i}"))
            .ToList();
        _memberRepoMock.Setup(r => r.GetMembersByOrganizationIdAsync(1))
            .ReturnsAsync(existingMembers);

        var act = () => _service.AddMemberToOrganizationAsync(1, "new-user",
            new AddOrganizationMemberRequest { Role = "orgUser" }, "admin");

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Basic*5*");
    }

    [Fact]
    public async Task AddMember_UnlimitedPlan_AllowsAddingBeyondDefault()
    {
        // Ultimate plan: -1 = unlimited
        var plan = TestDataBuilder.CreatePaymentPlan(id: 3, maxUsers: -1);
        await _context.PaymentPlans.AddAsync(plan);
        await _context.SaveChangesAsync();

        var org = TestDataBuilder.CreateOrganization(paymentPlanId: 3);

        _orgRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(org);
        _memberRepoMock.Setup(r => r.HasRoleAsync(1, "admin", "orgAdmin")).ReturnsAsync(true);
        _memberRepoMock.Setup(r => r.GetMembershipAsync(1, "new-user")).ReturnsAsync((OrganizationMember?)null);
        _memberRepoMock.Setup(r => r.AddMemberAsync(It.IsAny<OrganizationMember>()))
            .ReturnsAsync(TestDataBuilder.CreateMember(1, "new-user"));

        var act = () => _service.AddMemberToOrganizationAsync(1, "new-user",
            new AddOrganizationMemberRequest { Role = "orgUser" }, "admin");

        // Should NOT throw — unlimited plan allows any count
        await act.Should().NotThrowAsync();
    }

    // ─── Bank account business rules ─────────────────────────────────────────

    [Fact]
    public async Task DeleteBankAccount_ActiveAccount_ThrowsBusinessRuleException()
    {
        var activeAccount = TestDataBuilder.CreateBankAccount(id: 1, organizationId: 1, active: true);
        await _context.BankAccounts.AddAsync(activeAccount);
        await _context.SaveChangesAsync();

        var org = TestDataBuilder.CreateOrganization();
        _orgRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(org);

        var act = () => _service.DeleteBankAccountAsync(1, 1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*active*");
    }

    [Fact]
    public async Task AddBankAccount_FirstAccount_IsAutoActivated()
    {
        var org = TestDataBuilder.CreateOrganization();
        _orgRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(org);
        _orgRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);

        var request = new CreateBankAccountRequest
        {
            BankName = "FNB",
            BranchCode = "250655",
            AccountNumber = "62123456789",
            AccountType = "Cheque"
        };

        var result = await _service.AddBankAccountAsync(1, request);

        result.Active.Should().BeTrue("the first bank account should be automatically activated");
    }

    [Fact]
    public async Task SetActiveBankAccount_DeactivatesOtherAccounts()
    {
        var account1 = TestDataBuilder.CreateBankAccount(id: 1, organizationId: 1, active: true);
        var account2 = TestDataBuilder.CreateBankAccount(id: 2, organizationId: 1, active: false);
        await _context.BankAccounts.AddRangeAsync(account1, account2);
        await _context.SaveChangesAsync();

        var result = await _service.SetActiveBankAccountAsync(1, 2);

        result.Active.Should().BeTrue();

        var updatedAccount1 = await _context.BankAccounts.FindAsync(1);
        updatedAccount1!.Active.Should().BeFalse("setting account 2 active should deactivate account 1");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
