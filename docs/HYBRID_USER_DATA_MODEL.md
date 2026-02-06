# Hybrid User Data Model Architecture

## Overview

This system implements a **hybrid user data model** that correctly separates concerns between identity management (Keycloak) and business logic (application database). The architecture follows CQRS principles with separate read and write models.

## Core Principles

1. **Keycloak owns identity and access**: firstname, lastname, email, roles, enabled status
2. **App database owns business meaning and state**: active status, createdAt, updatedAt
3. **Read model (UserDirectory) is denormalized**: Combines both sources for fast queries
4. **Write operations are separated**: Identity changes go to Keycloak, state changes go to app DB

## Data Models

### 1. User (Write Model - App Database)

**Purpose**: Minimal table storing only business/state fields

**Location**: `management/backend/Models/User.cs`

**Fields**:
- `Id` (string): Keycloak user ID (UUID) - Primary Key
- `Active` (bool): Business state - whether user is active in the application
- `CreatedAt` (DateTime): When user was created in app database
- `UpdatedAt` (DateTime?): When user was last updated in app database

**Table**: `Users`

**Important**: This table does NOT store email, firstname, lastname, or roles. Those are owned by Keycloak.

### 2. UserDirectory (Read Model - App Database)

**Purpose**: Read-optimized projection combining Keycloak identity data with app DB business/state data

**Location**: `management/backend/Models/UserDirectory.cs`

**Fields**:
- `Id` (string): Keycloak user ID (UUID) - Primary Key
- **Identity data from Keycloak**:
  - `Email` (string): User's email address
  - `FirstName` (string?): User's first name
  - `LastName` (string?): User's last name
  - `Roles` (string?): Comma-separated list of roles
  - `KeycloakEnabled` (bool): Whether user is enabled in Keycloak
- **Business/state data from app DB**:
  - `Active` (bool): Whether user is active in the application
  - `CreatedAt` (DateTime): When user was created
  - `UpdatedAt` (DateTime?): When user was last updated
- **Sync metadata**:
  - `LastSyncedAt` (DateTime): When this entry was last synced from Keycloak

**Table**: `UserDirectory`

**Important**: This is a **read-only model**. Never write directly to this table except via sync operations.

## Services

### 1. UserService (Write Operations)

**Location**: `management/backend/Services/User/UserService.cs`

**Responsibilities**:
- Create users (writes to both Keycloak and app DB)
- Update users (routes identity changes to Keycloak, state changes to app DB)
- Delegates read operations to UserDirectoryService
- Triggers sync to UserDirectory after write operations

**Key Methods**:
- `CreateUserAsync()`: Creates user in Keycloak (identity) and app DB (state)
- `UpdateUserAsync()`: Updates Keycloak (identity fields) and/or app DB (state fields)
- Read methods delegate to UserDirectoryService

### 2. UserDirectoryService (Read Operations)

**Location**: `management/backend/Services/User/UserDirectoryService.cs`

**Responsibilities**:
- Query UserDirectory with pagination, filtering, sorting
- Sync users from Keycloak to UserDirectory
- Provide fast, denormalized queries for UI tables

**Key Methods**:
- `GetUsersAsync(query)`: Paginated, filtered, sorted user queries
- `GetUserByIdAsync(userId)`: Get single user from directory
- `SyncUserAsync(userId)`: Sync single user from Keycloak to directory
- `SyncAllUsersAsync()`: Full sync of all users

## API Endpoints

### ManagementBackend

**Write Endpoints**:
- `POST /api/users` - Create user (writes to Keycloak + app DB)
- `PUT /api/users/{id}` - Update user (routes to Keycloak and/or app DB)

**Read Endpoints**:
- `GET /api/users/directory` - Query UserDirectory with pagination/filtering/sorting
- `GET /api/users` - Get all users (delegates to UserDirectory)
- `GET /api/users/{id}` - Get single user (delegates to UserDirectory)

**Sync Endpoints**:
- `POST /api/users/directory/sync` - Trigger full sync from Keycloak to UserDirectory

### InvoiceTrackerApi

Similar endpoints should be implemented following the same pattern:
- Use UserDirectoryService for all read operations
- Use UserService for write operations
- Implement sync endpoints if needed

## Sync Mechanism

### Current Implementation: On-Demand Sync

**When sync happens**:
1. After user creation (`CreateUserAsync`)
2. After user update (`UpdateUserAsync`)
3. Manual trigger via `/api/users/directory/sync` endpoint

**How sync works** (`UserDirectoryService.SyncUserAsync`):
1. Fetch user from app database (User table)
2. Fetch user from Keycloak (via KeycloakAuthService)
3. Upsert to UserDirectory with combined data
4. Update `LastSyncedAt` timestamp

### Future Enhancement: Event-Driven Sync

**Recommended approach**:
1. Listen to Keycloak events (user created, updated, deleted)
2. Trigger sync automatically when identity data changes
3. Use message queue (RabbitMQ, Kafka) for reliable event processing

**Implementation steps**:
1. Configure Keycloak to emit events
2. Create background service to consume events
3. Call `UserDirectoryService.SyncUserAsync()` on events

### Alternative: Periodic Sync

**Approach**:
1. Create background service with timer
2. Run `SyncAllUsersAsync()` every N minutes
3. Track `LastSyncedAt` to identify stale entries

**Pros**: Simple, reliable
**Cons**: Eventually consistent with delay

## Write Operation Routing

### Identity Fields (Keycloak)
- `Email` → Keycloak only
- `FirstName` → Keycloak only
- `LastName` → Keycloak only
- `Roles` → Keycloak only
- `Enabled` → Keycloak only

### Business/State Fields (App DB)
- `Active` → App DB only
- `CreatedAt` → App DB only
- `UpdatedAt` → App DB only

### Example: Update User

```csharp
// Update identity fields (firstname, lastname) → Keycloak
await _keycloakService.UpdateUserAsync(userId, firstName, lastName, null, cancellationToken);

// Update business/state fields (active) → App DB
user.Active = request.Active.Value;
user.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync(cancellationToken);

// Sync to UserDirectory for reads
await _userDirectoryService.SyncUserAsync(userId, cancellationToken);
```

## Read Operation Pattern

**Always use UserDirectory for reads**:

```csharp
// ❌ WRONG: Querying User table (missing identity data)
var users = await _context.Users.ToListAsync();

// ✅ CORRECT: Querying UserDirectory (has all data)
var query = new UserDirectoryQuery { Page = 1, PageSize = 20 };
var result = await _userDirectoryService.GetUsersAsync(query);
```

## Database Schema

### Users Table (Write Model)
```sql
CREATE TABLE "Users" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Active" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP
);
```

### UserDirectory Table (Read Model)
```sql
CREATE TABLE "UserDirectory" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL,
    "FirstName" VARCHAR(100),
    "LastName" VARCHAR(100),
    "Roles" VARCHAR(500),
    "KeycloakEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "Active" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP,
    "LastSyncedAt" TIMESTAMP NOT NULL
);

CREATE INDEX "IX_UserDirectory_Email" ON "UserDirectory" ("Email");
CREATE INDEX "IX_UserDirectory_LastSyncedAt" ON "UserDirectory" ("LastSyncedAt");
```

## Migration Path

### From Old Model to New Model

**Migration**: `20260206124317_UpdateUserModelAndAddUserDirectory`

**Changes**:
1. Drop `Email`, `FirstName`, `LastName` columns from `Users` table
2. Drop unique index on `Email` in `Users` table
3. Create `UserDirectory` table with all fields
4. Add indexes on `Email` and `LastSyncedAt`

**Data Migration** (manual step after migration):
```csharp
// After running migration, sync all existing users
await _userDirectoryService.SyncAllUsersAsync();
```

## Benefits

1. **Clear separation of concerns**: Identity vs business logic
2. **Fast queries**: UserDirectory is denormalized for performance
3. **Scalability**: Read model can be optimized independently
4. **Flexibility**: Can add caching, materialized views, etc. to read model
5. **Consistency**: Single source of truth for identity (Keycloak) and state (app DB)
6. **CQRS compliance**: Separate read and write models

## Best Practices

1. **Never query User table for display**: Always use UserDirectory
2. **Never write to UserDirectory directly**: Only via sync operations
3. **Always sync after writes**: Ensure UserDirectory stays up-to-date
4. **Use pagination**: UserDirectory queries should always paginate
5. **Monitor sync lag**: Track `LastSyncedAt` to identify sync issues
6. **Handle sync failures gracefully**: Log errors, retry failed syncs

## Example Usage

### Creating a User
```csharp
var request = new CreateUserRequest
{
    Email = "user@example.com",
    FirstName = "John",
    LastName = "Doe",
    Role = UserRole.User,
    Active = true
};

// Creates in Keycloak (identity) and app DB (state), then syncs to UserDirectory
var user = await _userService.CreateUserAsync(request);
```

### Querying Users for UI Table
```csharp
var query = new UserDirectoryQuery
{
    Page = 1,
    PageSize = 20,
    SearchTerm = "john",
    SortBy = "Email",
    SortDescending = false,
    ActiveOnly = true
};

// Fast query from denormalized UserDirectory
var result = await _userDirectoryService.GetUsersAsync(query);
```

### Updating User Active Status
```csharp
var request = new UpdateUserRequest
{
    Active = false // Business state change
};

// Updates app DB, then syncs to UserDirectory
var user = await _userService.UpdateUserAsync(userId, request);
```

### Updating User Identity
```csharp
var request = new UpdateUserRequest
{
    FirstName = "Jane",
    LastName = "Smith"
};

// Updates Keycloak, then syncs to UserDirectory
var user = await _userService.UpdateUserAsync(userId, request);
```

## Troubleshooting

### UserDirectory is out of sync
**Solution**: Call `POST /api/users/directory/sync` to trigger full sync

### User not found in UserDirectory
**Solution**: User may not have been synced yet. Call `SyncUserAsync(userId)`

### Performance issues with queries
**Solution**: Add indexes to UserDirectory, implement caching, or use materialized views

### Sync failures
**Solution**: Check logs, verify Keycloak connectivity, retry failed syncs
