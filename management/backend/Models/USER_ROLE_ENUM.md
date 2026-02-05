# UserRole Enum - Single Source of Truth

## Overview
The `UserRole` enum is the single source of truth for all allowed user roles in the management application. It is defined in the backend and automatically included in the generated TypeScript API client for the frontend.

## Backend Definition
**Location:** `management/backend/Models/UserRole.cs`

```csharp
/// <summary>
/// Defines the allowed roles for users within an organization.
/// This is the single source of truth for user roles in the system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// Regular organization user with basic permissions
    /// </summary>
    OrgUser,
    
    /// <summary>
    /// Organization administrator with elevated permissions
    /// </summary>
    OrgAdmin
}
```

## Frontend Usage
The enum is automatically generated in the TypeScript API client at:
**Location:** `management/frontend/src/api/generated/api-client.ts`

```typescript
export type UserRole = "OrgUser" | "OrgAdmin";
```

### Example Usage in Frontend

```typescript
import { UserRole, CreateOrganizationMemberRequest } from '@/api/generated/api-client';

// Using the enum in a request
const newMember: CreateOrganizationMemberRequest = {
  Email: 'user@example.com',
  FirstName: 'John',
  LastName: 'Doe',
  Role: UserRole.OrgUser  // Type-safe, no hardcoded strings!
};

// Checking roles
if (member.Role === UserRole.OrgAdmin) {
  // User is an admin
}

// Creating a dropdown/select with all available roles
const roleOptions = [
  { value: UserRole.OrgUser, label: 'Organization User' },
  { value: UserRole.OrgAdmin, label: 'Organization Admin' }
];
```

## Benefits

1. **Type Safety**: The frontend gets full TypeScript type checking for roles
2. **Single Source of Truth**: Roles are defined once in the backend
3. **Automatic Synchronization**: Any changes to roles in the backend are automatically reflected in the frontend after regenerating the API client
4. **No Hardcoded Strings**: Eliminates typos and inconsistencies
5. **IDE Support**: Full autocomplete and IntelliSense support in the frontend

## Adding New Roles

To add a new role:

1. Add the new role to the `UserRole` enum in `management/backend/Models/UserRole.cs`
2. Create a database migration: `dotnet ef migrations add AddNewRole`
3. Regenerate the API client: `./generate-client.sh`
4. The new role is now available in both backend and frontend!

## Files Updated

- ✅ `management/backend/Models/UserRole.cs` - Enum definition
- ✅ `management/backend/Models/OrganizationMember.cs` - Uses UserRole enum
- ✅ `management/backend/DTOs/User/UserRequests.cs` - Uses UserRole enum
- ✅ `management/backend/DTOs/User/UserResponse.cs` - Uses UserRole enum
- ✅ `management/backend/Validators/User/CreateUserRequestValidator.cs` - Validates using enum
- ✅ `management/backend/Validators/User/CreateOrganizationMemberRequestValidator.cs` - Validates using enum
- ✅ `management/backend/Services/Auth/IKeycloakAuthService.cs` - Interface uses UserRole enum
- ✅ `management/backend/Services/Auth/KeycloakAuthService.cs` - Implementation uses UserRole enum
- ✅ `management/frontend/src/api/generated/api-client.ts` - Auto-generated TypeScript enum
