# Organization Context Implementation

## Overview
This document describes the implementation of organization context management in the client frontend application. When an admin user logs in, the application automatically fetches and stores organization data in a Pinia store.

## Security Considerations

**Important**: The `GET /api/organization` endpoint is user-scoped and only returns organizations that the authenticated user is a member of (based on the `OrganizationMembers` table). This ensures proper data isolation and multi-tenant security. The backend extracts the user ID from the JWT token and filters organizations accordingly.

## Implementation Details

### 1. Organization Store (`src/stores/organization.ts`)
A new Pinia store that manages organization context:

**State:**
- `organizationIds`: Array of organization IDs the admin belongs to
- `currentOrganization`: Details of the currently selected organization
- `isLoading`: Loading state indicator

**Key Actions:**
- `initializeOrganizationContext()`: Main function that:
  1. Fetches all organization IDs for the admin user
  2. Automatically fetches details for the first organization
  3. Stores both the list of IDs and the organization details
- `fetchOrganizationIds()`: Retrieves all organization IDs
- `fetchOrganizationDetails(id)`: Retrieves detailed information for a specific organization
- `switchOrganization(id)`: Allows switching between organizations
- `clearOrganizationContext()`: Clears organization data on logout

### 2. Organization API (`src/services/api.ts`)
Added `organizationApi` with the following endpoints:
- `getOrganizations()`: GET /api/organization - Fetch organizations the authenticated user belongs to (user-scoped via OrganizationMembers table)
- `getOrganization(id)`: GET /api/organization/{id} - Fetch specific organization details
- `createOrganization(data)`: POST /api/organization - Create new organization
- `updateOrganization(id, data)`: PUT /api/organization/{id} - Update organization
- `deleteOrganization(id)`: DELETE /api/organization/{id} - Delete organization

### 3. Auth Store Integration (`src/stores/auth.ts`)
Modified the `login()` function to:
- Import and use the organization store
- After successful admin login, call `initializeOrganizationContext()`
- Show a warning if organization context fails to load (but still allow login)

Modified the `logout()` function to:
- Clear organization context when user logs out

### 4. UI Integration (`src/views/AdminDashboard.vue`)
Updated the Admin Dashboard to:
- Import and use the organization store
- Display the current organization name in the header
- Provide visual feedback that organization context is loaded

## Usage

### Accessing Organization Context
```typescript
import { useOrganizationStore } from '@/stores/organization'

const organizationStore = useOrganizationStore()

// Get current organization
const currentOrg = organizationStore.currentOrganization

// Get all organization IDs
const orgIds = organizationStore.organizationIds

// Switch to a different organization
await organizationStore.switchOrganization(orgId)
```

### Organization Data Structure
```typescript
interface OrganizationResponse {
  id?: number
  name?: string
  address?: AddressResponse
  bankAccounts?: BankAccountResponse[]
}
```

## Flow Diagram

```
Admin Login
    ↓
Auth Store: login()
    ↓
API: POST /api/auth/admin/login
    ↓
Store tokens & user info
    ↓
Organization Store: initializeOrganizationContext()
    ↓
API: GET /api/organization
    ↓ (Backend extracts userId from JWT)
    ↓ (Queries OrganizationMembers table)
    ↓ (Returns only user's organizations)
Store organization IDs
    ↓
API: GET /api/organization/{firstId} (fetch first org details)
    ↓
Store current organization details
    ↓
Redirect to Admin Dashboard
    ↓
Display organization name in header
```

## Backend Implementation Details

The [`OrganizationController.GetOrganizations()`](apps/client/backend/InvoiceTrackerApi/Controllers/OrganizationController.cs:34) endpoint:
1. Extracts the authenticated user's ID from the JWT token via `GetUserId()` (inherited from `AuthenticatedControllerBase`)
2. Calls [`OrganizationService.GetUserOrganizationsAsync(userId)`](apps/client/backend/InvoiceTrackerApi/Services/Organization/OrganizationService.cs:325)
3. The service queries the `OrganizationMembers` table to find organizations where the user is a member
4. Returns only those organizations with full details (including bank accounts)

This ensures proper multi-tenant data isolation and security.

## Error Handling
- All API calls include comprehensive error handling
- Errors are displayed to users via toast notifications
- Failed organization context initialization doesn't prevent login
- Organization context is cleared on logout to prevent stale data

## Future Enhancements
- Add organization switcher UI component
- Persist selected organization in localStorage
- Add organization-specific filtering throughout the app
- Implement organization member management
