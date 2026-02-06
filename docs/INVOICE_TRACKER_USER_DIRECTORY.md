# InvoiceTrackerApi User Directory Implementation Guide

## Overview

This guide explains how to implement UserDirectory read model endpoints in InvoiceTrackerApi, following the same pattern as ManagementBackend.

## Current State

InvoiceTrackerApi currently does not have user management endpoints. It uses Keycloak for authentication but doesn't query user data directly.

## Implementation Steps

### 1. Add UserDirectory Model

Since both backends share the same database, the `UserDirectory` table already exists after running the ManagementBackend migration.

Create the model in InvoiceTrackerApi:

**File**: `backend/InvoiceTrackerApi/Models/UserDirectory.cs`

```csharp
namespace InvoiceTrackerApi.Models;

/// <summary>
/// UserDirectory read model - shared across all backends
/// </summary>
public class UserDirectory
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Roles { get; set; }
    public bool KeycloakEnabled { get; set; } = true;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
```

### 2. Add UserDirectory to DbContext

Update `backend/InvoiceTrackerApi/Data/ApplicationDbContext.cs`:

```csharp
public DbSet<UserDirectory> UserDirectory { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations ...
    
    // Configure UserDirectory (read-only)
    modelBuilder.Entity<UserDirectory>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email);
        entity.HasIndex(e => e.LastSyncedAt);
        
        entity.Property(e => e.Id)
            .IsRequired()
            .HasMaxLength(255);
        
        entity.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        entity.Property(e => e.FirstName)
            .HasMaxLength(100);
        
        entity.Property(e => e.LastName)
            .HasMaxLength(100);
        
        entity.Property(e => e.Roles)
            .HasMaxLength(500);
    });
}
```

### 3. Create UserDirectory Service

**File**: `backend/InvoiceTrackerApi/Services/IUserDirectoryService.cs`

```csharp
namespace InvoiceTrackerApi.Services;

public interface IUserDirectoryService
{
    Task<PagedUserDirectoryResponse> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken = default);
    
    Task<UserResponse> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public class UserDirectoryQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Email";
    public bool SortDescending { get; set; } = false;
    public bool? ActiveOnly { get; set; }
}

public class PagedUserDirectoryResponse
{
    public List<UserResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class UserResponse
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**File**: `backend/InvoiceTrackerApi/Services/UserDirectoryService.cs`

```csharp
using InvoiceTrackerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Services;

public class UserDirectoryService : IUserDirectoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserDirectoryService> _logger;

    public UserDirectoryService(
        ApplicationDbContext context,
        ILogger<UserDirectoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedUserDirectoryResponse> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.UserDirectory.AsQueryable();

        // Apply filters
        if (query.ActiveOnly.HasValue)
        {
            queryable = queryable.Where(u => u.Active == query.ActiveOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            queryable = queryable.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        // Apply sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "firstname" => query.SortDescending
                ? queryable.OrderByDescending(u => u.FirstName)
                : queryable.OrderBy(u => u.FirstName),
            "lastname" => query.SortDescending
                ? queryable.OrderByDescending(u => u.LastName)
                : queryable.OrderBy(u => u.LastName),
            "createdat" => query.SortDescending
                ? queryable.OrderByDescending(u => u.CreatedAt)
                : queryable.OrderBy(u => u.CreatedAt),
            _ => query.SortDescending
                ? queryable.OrderByDescending(u => u.Email)
                : queryable.OrderBy(u => u.Email)
        };

        var users = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Active = u.Active,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedUserDirectoryResponse
        {
            Users = users,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UserResponse> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserDirectory
            .Where(u => u.Id == userId)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Active = u.Active,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        return user;
    }
}
```

### 4. Create API Endpoints

**File**: `backend/InvoiceTrackerApi/Controllers/UserController.cs`

```csharp
using InvoiceTrackerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : AuthenticatedControllerBase
{
    private readonly IUserDirectoryService _userDirectoryService;

    public UserController(IUserDirectoryService userDirectoryService)
    {
        _userDirectoryService = userDirectoryService;
    }

    /// <summary>
    /// Get users from UserDirectory with pagination and filtering
    /// </summary>
    [HttpGet("directory")]
    [ProducesResponseType(typeof(PagedUserDirectoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDirectory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = "Email",
        [FromQuery] bool sortDescending = false,
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new UserDirectoryQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDescending = sortDescending,
            ActiveOnly = activeOnly
        };

        var result = await _userDirectoryService.GetUsersAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a single user by ID from UserDirectory
    /// </summary>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userDirectoryService.GetUserByIdAsync(userId, cancellationToken);
        return Ok(user);
    }
}
```

### 5. Register Service in DI Container

Update `backend/InvoiceTrackerApi/Program.cs`:

```csharp
// Add after other service registrations
builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();
```

### 6. Update Swagger Configuration

Ensure Swagger is configured to document the new endpoints in `Program.cs`.

## Important Notes

### Read-Only Access

InvoiceTrackerApi should **only read** from UserDirectory. It should NOT:
- Create users
- Update users
- Sync users

All write operations and sync operations are handled by ManagementBackend.

### Shared Database

Both backends share the same database, so:
- No migration needed in InvoiceTrackerApi
- UserDirectory table already exists
- Both backends can query the same UserDirectory data

### Authentication

The endpoints should be protected with appropriate authorization:
- Use `[Authorize]` attribute
- Consider role-based access if needed
- Ensure only authenticated users can query user data

## Testing

### 1. Verify UserDirectory is populated

First, ensure ManagementBackend has synced users:
```bash
POST http://localhost:5002/api/users/directory/sync
```

### 2. Query users from InvoiceTrackerApi

```bash
GET http://localhost:5001/api/user/directory?page=1&pageSize=20
```

### 3. Get specific user

```bash
GET http://localhost:5001/api/user/{userId}
```

## Benefits

1. **Consistent data model**: Both backends use the same UserDirectory structure
2. **Fast queries**: No runtime Keycloak calls needed
3. **Separation of concerns**: ManagementBackend handles writes, InvoiceTrackerApi only reads
4. **Scalability**: Read model can be cached or replicated independently

## See Also

- [Hybrid User Data Model Architecture](./HYBRID_USER_DATA_MODEL.md) - Complete architecture documentation
- ManagementBackend implementation - Reference implementation
