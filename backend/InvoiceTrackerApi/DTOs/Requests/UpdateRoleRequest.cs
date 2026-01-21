using System.Text.Json.Serialization;
using InvoiceTrackerApi.Services.Auth;

namespace InvoiceTrackerApi.DTOs.Requests;

/// <summary>
/// Request model for updating user role
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// The role to assign to the user. Valid values: OrgUser, OrgAdmin (SystemAdmin can only be assigned in Keycloak)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; set; }
}
