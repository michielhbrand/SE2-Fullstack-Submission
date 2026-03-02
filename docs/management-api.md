# ManagementApi — Design Decisions

## Request Pipeline

```
  HTTP Request
       │
       ▼
┌──────────────────┐
│  Rate Limiting   │──── 429 Too Many Requests
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Authentication  │──── 401 Unauthorized
│  (JWT Bearer)    │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  ValidationFilter│──── 400 Bad Request (ProblemDetails)
│  (FluentValid.)  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Endpoint Handler│──── 200/201 Success
│  (Minimal API)   │──── 404/500 (GlobalExceptionHandler)
└──────────────────┘
```

## Minimal APIs (vs Traditional Controllers)

- **Minimal API** pattern chosen deliberately to contrast with the InvoiceTrackerApi's controller-based approach
- Demonstrates proficiency with both .NET API styles in the same project
- Benefits of Minimal APIs for this use case:
  - Fewer files and less ceremony — ideal for a focused admin API with a smaller surface area
  - Each endpoint is a self-contained static class — easy to locate and reason about
  - Direct handler functions with typed parameters — no controller inheritance overhead
  - Built-in support for `TypedResults` providing compile-time response type safety

## Endpoint Organization

- Endpoints grouped by domain: `Auth/`, `Organization/`, `User/`
- Each endpoint is a **single static class** with a `MapXxx()` extension method and a private `Handle()` method
- Configuration classes (`AuthEndpointsConfiguration`, `OrganizationEndpointsConfiguration`) compose endpoints into route groups
- This pattern keeps each endpoint focused on one responsibility while maintaining clean route registration in `Program.cs`

## FluentValidation

- **FluentValidation** chosen over Data Annotations for request validation
- Advantages:
  - Validation rules are separate from DTOs — DTOs stay clean data contracts
  - Complex validation logic (conditional rules, cross-field validation) expressed fluently
  - Validators are unit-testable in isolation
  - `AddValidatorsFromAssemblyContaining<Program>()` auto-discovers all validators — no manual registration
- Dedicated `Validators/` folder organized by domain (Auth, Organization, User)

## Validation Filter

- Custom `ValidationFilter<T>` implements `IEndpointFilter` — the Minimal API equivalent of action filters
- Intercepts requests before the handler, runs FluentValidation, and returns RFC 9457 `ValidationProblemDetails` on failure
- Applied per-endpoint via `.AddEndpointFilter<ValidationFilter<T>>()` — explicit about which endpoints are validated
- Reusable generic implementation — works with any request DTO type

## Rate Limiting

- **AspNetCoreRateLimit** middleware protects the management API from abuse
- IP-based rate limiting configured via `appsettings.json` — no code changes needed to adjust limits
- Memory cache used for rate limit counters — lightweight for single-instance deployment
- Rate limiting middleware placed **before** authentication in the pipeline — blocks abusive requests before token validation overhead
- Important for a management API that handles sensitive operations (user creation, role changes)

## Exception Handling

- Same **global exception handler** pattern as InvoiceTrackerApi — consistent error responses across both APIs
- RFC 9457 Problem Details for all error responses
- Exception types sourced from the shared **`Shared.Core`** library (`Shared.Core.Exceptions.Application`):
  - `NotFoundException` (404), `ValidationException` (400), `UnauthorizedException` (401), `ForbiddenException` (403), `ServiceUnavailableException` (503)
- **`GlobalExceptionHandler`** includes `traceId` (from `httpContext.TraceIdentifier`) in all `ProblemDetails` responses — this is part of a consistent cross-service pattern shared with `InvoiceTrackerApi`
- **Serilog** with `CompactJsonFormatter` writes structured JSON to stdout, enriched with `ServiceName`, `Environment`, `MachineName`, and `ThreadId`; `Serilog.Enrichers.Span` embeds `TraceId` and `SpanId` into every log entry

## Authentication & Authorization

- Same **Keycloak JWT Bearer** authentication as InvoiceTrackerApi — shared identity provider
- Separate CORS policy (`AllowManagementPortal`) — only the management frontend origin is permitted
- `systemAdmin` role required for management operations — enforced at the endpoint level

## User Provisioning

- New users are created via the **Keycloak Admin REST API** (`KeycloakUserAdminService`) — the management backend provisions the identity directly in Keycloak, no self-registration required
- Default password for all new users is `password123` — a fixed password used regardless of the user's email address, which avoids failures when an email address is shorter than Keycloak's minimum password length
- Users can change their password via the Keycloak account portal at any time
- `GetOrganizationsRequest` uses a plain `class` with regular `set` properties (not a `record` with `init`) — required for correct `[AsParameters]` query-string binding in .NET 8 when optional integer parameters are absent from the request

## Testing Strategy

- **Comprehensive test suite** with both unit and integration tests
- Unit tests cover:
  - Mappers — verify DTO ↔ Entity transformations
  - Validators — verify all validation rules with valid and invalid inputs
  - Services — verify business logic with mocked dependencies
- Integration tests use:
  - `TestWebApplicationFactory` — custom `WebApplicationFactory<Program>` with in-memory database and test auth handler
  - `TestAuthHandler` — bypasses Keycloak for testing, injects configurable claims
  - `TestDataBuilder` — fluent builder for seeding test data
- `public partial class Program { }` in `Program.cs` — makes the entry point accessible to `WebApplicationFactory`
- `coverlet.runsettings` configured for code coverage reporting

## Direct DbContext Access

- Management API accesses `ApplicationDbContext` directly in endpoint handlers — no repository layer
- Deliberate choice for a simpler API with straightforward CRUD operations
- Reduces abstraction overhead where the repository pattern would add complexity without benefit
- The shared `ApplicationDbContext` from `Shared.Database` ensures schema consistency

## Mapper Pattern

- Static mapper classes (`OrganizationMapper`, `UserMapper`, `AuthMapper`, `UserDirectoryMapper`) with extension methods
- Same compile-time safe, explicit mapping approach as InvoiceTrackerApi
- Keeps mapping logic centralized and testable

## OpenAPI & Client Generation

- NSwag generates OpenAPI spec from endpoint metadata (`.WithOpenApi()`, `.Produces<T>()`, `.ProducesProblem()`)
- TypeScript API client auto-generated for the management frontend
- `.WithName()`, `.WithSummary()`, `.WithDescription()` provide rich API documentation
