using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using InvoiceTrackerApi.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Organization management endpoints
/// </summary>
[ApiController]
[Route("api/organization")]
[Produces("application/json")]
[Authorize]
public class OrganizationController : AuthenticatedControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(IOrganizationService organizationService, ILogger<OrganizationController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all organizations for the authenticated user
    /// </summary>
    /// <returns>List of organizations the user belongs to</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrganizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<OrganizationResponse>>> GetOrganizations()
    {
        var userId = GetCurrentUserId();
        var organizations = await _organizationService.GetUserOrganizationsAsync(userId);
        return Ok(organizations);
    }

    /// <summary>
    /// Get a specific organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrganizationResponse>> GetOrganization(int id)
    {
        var organization = await _organizationService.GetOrganizationByIdAsync(id);
        return Ok(organization);
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    /// <param name="request">Organization creation data</param>
    /// <returns>Created organization</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrganizationResponse>> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var organization = await _organizationService.CreateOrganizationAsync(request);

        return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
    }

    /// <summary>
    /// Update an existing organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Updated organization data</param>
    /// <returns>Updated organization</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrganizationResponse>> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var organization = await _organizationService.UpdateOrganizationAsync(id, request);

        return Ok(organization);
    }

    /// <summary>
    /// Delete an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteOrganization(int id)
    {
        await _organizationService.DeleteOrganizationAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Add a bank account to an organization
    /// </summary>
    [HttpPost("{id}/bank-accounts")]
    [ProducesResponseType(typeof(BankAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BankAccountResponse>> AddBankAccount(int id, [FromBody] CreateBankAccountRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var bankAccount = await _organizationService.AddBankAccountAsync(id, request);
        return CreatedAtAction(nameof(GetOrganization), new { id }, bankAccount);
    }

    /// <summary>
    /// Delete a bank account from an organization
    /// </summary>
    [HttpDelete("{id}/bank-accounts/{accountId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteBankAccount(int id, int accountId)
    {
        await _organizationService.DeleteBankAccountAsync(id, accountId);
        return NoContent();
    }

    /// <summary>
    /// Set a bank account as the active account for an organization
    /// </summary>
    [HttpPut("{id}/bank-accounts/{accountId}/set-active")]
    [ProducesResponseType(typeof(BankAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BankAccountResponse>> SetActiveBankAccount(int id, int accountId)
    {
        var bankAccount = await _organizationService.SetActiveBankAccountAsync(id, accountId);
        return Ok(bankAccount);
    }
}
