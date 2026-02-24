using System.Text.Json.Serialization;

namespace Shared.Database.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    OrgUser,
    OrgAdmin,
    SystemAdmin
}
