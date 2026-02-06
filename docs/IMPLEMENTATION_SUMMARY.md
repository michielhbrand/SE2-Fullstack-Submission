# Hybrid User Data Model - Implementation Summary

## What Was Implemented

### ✅ Core Architecture

1. **Minimal User Model (Write Model)**
   - Removed identity fields (email, firstname, lastname) from app database
   - Kept only business/state fields: `active`, `createdAt`, `updatedAt`
   - Location: [`management/backend/Models/User.cs`](../management/backend/Models/User.cs)

2. **UserDirectory Read Model**
   - Created denormalized table combining Keycloak + app DB data
   - Includes identity fields (email, firstname, lastname, roles) + state fields
   - Optimized for fast queries with indexes
   - Location: [`management/backend/Models/UserDirectory.cs`](../management/backend/Models/UserDirectory.cs)

3. **Database Migration**
   - Migration: `20260206124317_UpdateUserModelAndAddUserDirectory`
   - Drops identity columns from Users table
   - Creates UserDirectory table with indexes
   - Location: [`management/backend/Migrations/`](../management/backend/Migrations/)

### ✅ Services

1. **UserDirectoryService (Read Operations)**
   - Queries UserDirectory with pagination, filtering, sorting
   - Syncs users from Keycloak to UserDirectory
   - Location: [`management/backend/Services/User/UserDirectoryService.cs`](../management/backend/Services/User/UserDirectoryService.cs)

2. **Updated UserService (Write Operations)**
   - Routes identity changes to Keycloak
   - Routes state changes to app DB
   - Triggers sync after writes
   - Location: [`management/backend/Services/User/UserService.cs`](../management/backend/Services/User/UserService.cs)

### ✅ API Endpoints (ManagementBackend)

**Read Endpoints:**
- `GET /api/users/directory` - Query with pagination/filtering/sorting
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get single user

**Write Endpoints:**
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user

**Sync Endpoints:**
- `POST /api/users/directory/sync` - Trigger full sync

### ✅ Documentation

1. **Architecture Documentation**: [`docs/HYBRID_USER_DATA_MODEL.md`](./HYBRID_USER_DATA_MODEL.md)
2. **InvoiceTrackerApi Guide**: [`docs/INVOICE_TRACKER_USER_DIRECTORY.md`](./INVOICE_TRACKER_USER_DIRECTORY.md)

## Next Steps

### 1. Apply Database Migration

Run the migration to update the database schema:

```bash
cd management/backend
dotnet ef database update
```

### 2. Initial Sync

After migration, sync existing users to UserDirectory:

```bash
# Start ManagementBackend
dotnet run

# Trigger sync (requires authentication)
POST http://localhost:5002/api/users/directory/sync
Authorization: Bearer {your-token}
```

### 3. Implement in InvoiceTrackerApi (Optional)

Follow the guide in [`docs/INVOICE_TRACKER_USER_DIRECTORY.md`](./INVOICE_TRACKER_USER_DIRECTORY.md) to add read-only UserDirectory endpoints to InvoiceTrackerApi.

**Key steps:**
1. Add UserDirectory model
2. Update DbContext
3. Create UserDirectoryService (read-only)
4. Add API endpoints
5. Register service in DI

### 4. Update Frontend (If Needed)

If your frontend queries user data, update it to use the new endpoints:

```typescript
// Old: GET /api/users
// New: GET /api/users/directory?page=1&pageSize=20&searchTerm=john

interface UserDirectoryQuery {
  page: number;
  pageSize: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
  activeOnly?: boolean;
}

interface PagedUserDirectoryResponse {
  users: UserResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

### 5. Implement Event-Driven Sync (Recommended)

Current implementation uses on-demand sync. For production, consider:

**Option A: Keycloak Events**
1. Configure Keycloak to emit events
2. Create event consumer service
3. Trigger sync on user changes

**Option B: Periodic Sync**
1. Create background service with timer
2. Run `SyncAllUsersAsync()` every N minutes
3. Monitor `LastSyncedAt` for stale entries

**Option C: Webhook-Based Sync**
1. Expose webhook endpoint
2. Configure Keycloak to call webhook on changes
3. Trigger sync from webhook

### 6. Add Monitoring

Track sync health:
- Monitor `LastSyncedAt` timestamps
- Alert on sync failures
- Log sync operations
- Track sync duration

### 7. Testing Checklist

- [ ] Migration runs successfully
- [ ] Initial sync populates UserDirectory
- [ ] Create user writes to Keycloak + app DB + syncs to UserDirectory
- [ ] Update user routes correctly (identity → Keycloak, state → app DB)
- [ ] Read operations query UserDirectory only
- [ ] Pagination works correctly
- [ ] Filtering works correctly
- [ ] Sorting works correctly
- [ ] Sync endpoint works
- [ ] Error handling works

## Key Principles to Remember

### ✅ DO

- **Always read from UserDirectory** for display/queries
- **Write identity fields to Keycloak** (email, firstname, lastname, roles)
- **Write state fields to app DB** (active, createdAt, updatedAt)
- **Sync after every write** to keep UserDirectory up-to-date
- **Use pagination** for all UserDirectory queries
- **Monitor sync lag** via `LastSyncedAt`

### ❌ DON'T

- **Don't query User table for display** (missing identity data)
- **Don't write to UserDirectory directly** (only via sync)
- **Don't store identity data in User table** (Keycloak owns it)
- **Don't skip sync after writes** (UserDirectory will be stale)
- **Don't query Keycloak at runtime** for display (use UserDirectory)

## Architecture Benefits

1. **Clear Separation**: Identity (Keycloak) vs Business Logic (App DB)
2. **Fast Queries**: Denormalized UserDirectory for performance
3. **Scalability**: Read model optimized independently
4. **CQRS Compliance**: Separate read and write models
5. **Flexibility**: Can add caching, materialized views, etc.
6. **Single Source of Truth**: Keycloak for identity, App DB for state

## Files Changed

### Created
- `management/backend/Models/UserDirectory.cs`
- `management/backend/Services/User/IUserDirectoryService.cs`
- `management/backend/Services/User/UserDirectoryService.cs`
- `management/backend/Extensions/UserDirectoryMappingExtensions.cs`
- `management/backend/Endpoints/User/GetUserDirectoryEndpoint.cs`
- `management/backend/Endpoints/User/SyncUserDirectoryEndpoint.cs`
- `management/backend/Migrations/20260206124317_UpdateUserModelAndAddUserDirectory.cs`
- `docs/HYBRID_USER_DATA_MODEL.md`
- `docs/INVOICE_TRACKER_USER_DIRECTORY.md`

### Modified
- `management/backend/Models/User.cs` - Removed identity fields
- `management/backend/Services/User/UserService.cs` - Separated write operations
- `management/backend/Extensions/UserMappingExtensions.cs` - Updated for new model
- `management/backend/Data/ApplicationDbContext.cs` - Added UserDirectory DbSet
- `management/backend/Endpoints/User/MapUserEndpoints.cs` - Added new endpoints
- `management/backend/Program.cs` - Registered UserDirectoryService

## Support

For questions or issues:
1. Review [`docs/HYBRID_USER_DATA_MODEL.md`](./HYBRID_USER_DATA_MODEL.md) for architecture details
2. Check troubleshooting section in documentation
3. Review example usage patterns
4. Verify sync is working via `LastSyncedAt` timestamps

## Success Criteria

✅ User table contains only: `Id`, `Active`, `CreatedAt`, `UpdatedAt`
✅ UserDirectory table contains all fields (identity + state)
✅ Write operations route correctly (Keycloak vs app DB)
✅ Read operations use UserDirectory only
✅ Sync mechanism populates UserDirectory
✅ API endpoints work with pagination/filtering/sorting
✅ Both backends can query UserDirectory (ManagementBackend implemented, InvoiceTrackerApi documented)
