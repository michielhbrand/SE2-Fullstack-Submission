using System.Text.Json.Serialization;

namespace ManagementApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    OrgUser,
    OrgAdmin,
    SystemAdmin
}
