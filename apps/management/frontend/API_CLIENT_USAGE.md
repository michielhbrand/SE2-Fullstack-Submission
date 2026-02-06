# Management Frontend - API Client Usage

This document explains how to use the generated TypeScript API client in the management frontend.

## Overview

The frontend uses a TypeScript API client generated from the backend's OpenAPI specification using NSwag. This provides type-safe API calls with full IntelliSense support.

## Architecture

### Files Structure

```
src/
├── api/
│   ├── generated/
│   │   └── api-client.ts          # Auto-generated API client (DO NOT EDIT)
│   └── client.ts                   # Configured API client instance with interceptors
├── services/
│   ├── organizations.ts            # Organization service wrapper
│   └── api.ts                      # Legacy API client (deprecated)
└── stores/
    └── auth.ts                     # Auth store using the API client
```

## API Client Setup

### 1. Generated Client

The [`api-client.ts`](src/api/generated/api-client.ts) file is auto-generated and should **never be edited manually**. To regenerate it:

```bash
cd management/backend
./generate-client.sh
```

This requires:
- Backend running at `http://localhost:5002`
- NSwag CLI tool installed: `dotnet tool install -g NSwag.ConsoleCore`

### 2. Configured Client Instance

The [`client.ts`](src/api/client.ts) file exports a configured instance of the API client with:

- **Base URL** from environment variables
- **Request interceptor** to add authentication tokens
- **Response interceptor** to handle token refresh on 401 errors

```typescript
import { apiClient } from '../api/client'

// Use the configured client instance
const organizations = await apiClient.getOrganizations()
```

## Usage Examples

### Authentication

The auth store ([`stores/auth.ts`](src/stores/auth.ts)) demonstrates authentication:

```typescript
import { apiClient } from '../api/client'

// Login
const response = await apiClient.adminLogin({
  Username: 'admin',
  Password: 'password',
})

// Access tokens
const { AccessToken, RefreshToken } = response

// Logout
await apiClient.logout({
  RefreshToken: refreshToken,
})
```

### Organization Management

The organization service ([`services/organizations.ts`](src/services/organizations.ts)) provides a clean API:

```typescript
import { organizationService } from '../services/organizations'

// Get all organizations
const organizations = await organizationService.getAll()

// Get by ID
const org = await organizationService.getById(1)

// Create organization
const newOrg = await organizationService.create({
  Name: 'Acme Corp',
  VatNumber: 'VAT123',
  RegistrationNumber: 'REG456',
  Address: {
    Street: '123 Main St',
    City: 'Cape Town',
    PostalCode: '8001',
    Country: 'South Africa',
  },
})

// Update organization
const updated = await organizationService.update(1, {
  Name: 'Acme Corporation',
})

// Delete organization
await organizationService.delete(1)
```

### Direct API Client Usage

You can also use the API client directly in components:

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { apiClient } from '../api/client'
import type { OrganizationResponse } from '../api/generated/api-client'

const organizations = ref<OrganizationResponse[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  loading.value = true
  try {
    organizations.value = await apiClient.getOrganizations()
  } catch (err: any) {
    error.value = err.message || 'Failed to load organizations'
  } finally {
    loading.value = false
  }
})
</script>
```

## Available API Methods

The generated client includes methods for all backend endpoints:

### Authentication
- `adminLogin(request: LoginRequest): Promise<LoginResponse>`
- `refreshToken(request: RefreshTokenRequest): Promise<LoginResponse>`
- `logout(request: LogoutRequest): Promise<void>`

### Organizations
- `getOrganizations(): Promise<OrganizationResponse[]>`
- `createOrganization(request: CreateOrganizationRequest): Promise<OrganizationResponse>`
- `updateOrganization(id: number, request: UpdateOrganizationRequest): Promise<void>`
- `deleteOrganization(id: number): Promise<void>`

## Type Definitions

All request and response types are exported from the generated client:

```typescript
import type {
  LoginRequest,
  LoginResponse,
  OrganizationResponse,
  CreateOrganizationRequest,
  UpdateOrganizationRequest,
  CreateAddressRequest,
  AddressResponse,
  BankAccountResponse,
} from '../api/generated/api-client'
```

## Error Handling

The API client throws `ApiException` for errors:

```typescript
import { apiClient, ApiException } from '../api/client'

try {
  await apiClient.createOrganization(data)
} catch (error) {
  if (error instanceof ApiException) {
    console.error('API Error:', error.status, error.message)
    console.error('Response:', error.response)
  } else {
    console.error('Unexpected error:', error)
  }
}
```

## Authentication Flow

1. **Login**: User credentials are sent to `/api/v1/auth/admin-login`
2. **Token Storage**: Access and refresh tokens are stored in localStorage
3. **Request Interceptor**: Adds `Authorization: Bearer <token>` to all requests
4. **Token Refresh**: On 401 errors, automatically refreshes the token using `/api/v1/auth/refresh`
5. **Logout**: Clears tokens and redirects to login page

## Best Practices

### 1. Use Service Wrappers

Create service wrappers for complex operations:

```typescript
// services/organizations.ts
export const organizationService = {
  async getAll() {
    return await apiClient.getOrganizations()
  },
  // ... more methods
}
```

### 2. Handle Errors Gracefully

Always wrap API calls in try-catch blocks:

```typescript
try {
  const result = await apiClient.someMethod()
  // Handle success
} catch (error) {
  // Handle error
  console.error('API call failed:', error)
}
```

### 3. Use TypeScript Types

Leverage the generated types for type safety:

```typescript
import type { OrganizationResponse } from '../api/generated/api-client'

const org: OrganizationResponse = {
  Id: 1,
  Name: 'Acme Corp',
  // TypeScript will enforce correct structure
}
```

### 4. Don't Edit Generated Files

Never modify [`api-client.ts`](src/api/generated/api-client.ts) directly. Changes will be overwritten when regenerating.

## Regenerating the Client

When the backend API changes:

1. Ensure the backend is running: `cd management/backend && dotnet run`
2. Run the generation script: `./generate-client.sh`
3. Review the changes in [`api-client.ts`](src/api/generated/api-client.ts)
4. Update service wrappers if needed

## Migration from Old API Client

The old axios-based API client ([`services/api.ts`](src/services/api.ts)) is deprecated. To migrate:

**Before:**
```typescript
import apiClient from '../services/api'

const response = await apiClient.post('/api/auth/admin-login', {
  username: 'admin',
  password: 'password',
})
```

**After:**
```typescript
import { apiClient } from '../api/client'

const response = await apiClient.adminLogin({
  Username: 'admin',
  Password: 'password',
})
```

## Environment Variables

Configure the API base URL in [`.env`](.env):

```env
VITE_API_URL=http://localhost:5002
```

## Troubleshooting

### Client Generation Fails

- Ensure backend is running at `http://localhost:5002`
- Check that NSwag is installed: `dotnet tool list -g`
- Verify OpenAPI spec is accessible: `curl http://localhost:5002/swagger/v1/swagger.json`

### 401 Unauthorized Errors

- Check that tokens are stored in localStorage
- Verify token hasn't expired
- Ensure request interceptor is adding the Authorization header

### Type Errors

- Regenerate the client if backend API has changed
- Check that you're using the correct property names (PascalCase from C#)

## Additional Resources

- [NSwag Documentation](https://github.com/RicoSuter/NSwag)
- [Axios Documentation](https://axios-http.com/)
- [Vue 3 Composition API](https://vuejs.org/guide/extras/composition-api-faq.html)
