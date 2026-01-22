using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using InvoiceTrackerApi.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Controller for managing organization membership (Keycloak users to organizations)
/// </summary>
[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationMemberController : AuthenticatedControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<OrganizationMemberController> _logger;

    public OrganizationMemberController(
        IOrganizationService organizationService,
        ILogger<OrganizationMemberController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// Add a user to an organization with a specific role
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="userId">Keycloak user ID (sub claim)</param>
    /// <param name="request">Member role information</param>
    /// <returns>Created membership</returns>
    [HttpPost("{orgId}/members/{userId}")]
    [ProducesResponseType(typeof(OrganizationMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationMemberResponse>> AddMember(
        int orgId, 
        string userId, 
        [FromBody] AddOrganizationMemberRequest request)
    {
        var requestingUserId = GetCurrentUserId();
        var member = await _organizationService.AddMemberToOrganizationAsync(
            orgId, userId, request, requestingUserId);

        return CreatedAtAction(
            nameof(GetOrganizationMembers), 
            new { orgId }, 
            member);
    }

    /// <summary>
    /// Remove a user from an organization
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="userId">Keycloak user ID (sub claim)</param>
    [HttpDelete("{orgId}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(int orgId, string userId)
    {
        var requestingUserId = GetCurrentUserId();
        await _organizationService.RemoveMemberFromOrganizationAsync(
            orgId, userId, requestingUserId);

        return NoContent();
    }

    /// <summary>
    /// Update a member's role within an organization
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="userId">Keycloak user ID (sub claim)</param>
    /// <param name="request">New role information</param>
    /// <returns>Updated membership</returns>
    [HttpPut("{orgId}/members/{userId}/role")]
    [ProducesResponseType(typeof(OrganizationMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationMemberResponse>> UpdateMemberRole(
        int orgId, 
        string userId, 
        [FromBody] UpdateMemberRoleRequest request)
    {
        var requestingUserId = GetCurrentUserId();
        var member = await _organizationService.UpdateMemberRoleAsync(
            orgId, userId, request, requestingUserId);

        return Ok(member);
    }

    /// <summary>
    /// Get all members of an organization
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <returns>List of organization members</returns>
    [HttpGet("{orgId}/members")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<OrganizationMemberResponse>>> GetOrganizationMembers(int orgId)
    {
        var members = await _organizationService.GetOrganizationMembersAsync(orgId);
        return Ok(members);
    }

    /// <summary>
    /// Get all organizations for the current authenticated user
    /// </summary>
    /// <returns>List of organizations the user belongs to</returns>
    [HttpGet("~/api/users/me/organizations")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrganizationResponse>>> GetMyOrganizations()
    {
        var userId = GetCurrentUserId();
        var organizations = await _organizationService.GetUserOrganizationsAsync(userId);
        return Ok(organizations);
    }
}
