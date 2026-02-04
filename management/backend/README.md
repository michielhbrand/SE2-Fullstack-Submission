# Management API

ASP.NET Core Web API for managing organizations in the system. This API is designed for system administrators to manage organizations and their members.

## Tech Stack

- **ASP.NET Core 8.0** - Web framework
- **Entity Framework Core** - ORM
- **PostgreSQL** - Database (shared with InvoiceTrackerApi)
- **Keycloak** - Authentication and authorization
- **NSwag** - OpenAPI/Swagger documentation
- **Minimal APIs** - Lightweight API endpoints

## Features

- System Admin only access (role-based authorization)
- Organization CRUD operations
- Address management
- Bank account management
- Organization member management
- Health checks
- Global exception handling
- JWT Bearer authentication

## Architecture

This API follows the same robust design patterns as the InvoiceTrackerApi:

- **Minimal APIs** - Uses ASP.NET Core Minimal APIs instead of traditional controllers
- **Extension Methods** - Service configuration organized in extension methods
- **Global Exception Handling** - Centralized error handling with RFC 9457 Problem Details
- **Repository Pattern** - Data access abstraction (implemented inline in minimal APIs for simplicity)
- **DTOs** - Request/Response data transfer objects
- **Database Integration** - Shares the same PostgreSQL database as InvoiceTrackerApi
- **Keycloak Integration** - JWT token validation and role extraction

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL (shared with InvoiceTrackerApi)
- Keycloak instance running

### Configuration

Update `appsettings.json` with your configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
  },
  "Keycloak": {
    "Authority": "http://localhost:9090/realms/microservices",
    "Audience": "backend-api",
    "ClientId": "management-portal"
  }
}
```

### Running the API

```bash
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5002`

### API Documentation

When running in development mode, access the Swagger UI at:
- Swagger UI: `http://localhost:5002/swagger`
- OpenAPI spec: `http://localhost:5002/swagger/v1/swagger.json`

## API Endpoints

### Organizations

All endpoints require `systemAdmin` role.

- `GET /api/organizations` - Get all organizations
- `GET /api/organizations/{id}` - Get organization by ID
- `POST /api/organizations` - Create new organization
- `PUT /api/organizations/{id}` - Update organization
- `DELETE /api/organizations/{id}` - Delete organization

### Health

- `GET /health` - Health check endpoint

## Authentication

All API endpoints (except `/health`) require a valid JWT token with the `systemAdmin` role.

Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

## Database

This API shares the same PostgreSQL database (`authdb`) with the InvoiceTrackerApi. It accesses the following tables:

- `Organizations`
- `Addresses`
- `BankAccounts`
- `OrganizationMembers`

No migrations are needed as the database schema is managed by InvoiceTrackerApi.

## Error Handling

The API uses RFC 9457 Problem Details for error responses:

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Organization with identifier '123' was not found.",
  "instance": "/api/organizations/123"
}
```

## Development

### Project Structure

```
management/backend/
├── Data/                   # Database context
├── DTOs/                   # Data transfer objects
├── Exceptions/             # Custom exceptions
├── Extensions/             # Service extensions
├── Models/                 # Entity models
├── Program.cs              # Application entry point with minimal APIs
├── appsettings.json        # Configuration
└── ManagementApi.csproj    # Project file
```

### Adding New Endpoints

Add new minimal API endpoints in `Program.cs`:

```csharp
app.MapGet("/api/example", async (ApplicationDbContext db) =>
{
    // Your logic here
    return Results.Ok(data);
})
.RequireAuthorization("SystemAdminOnly")
.WithName("ExampleEndpoint")
.WithOpenApi();
```

## License

Private - Internal use only
