# InvoiceTrackerApi — Design Decisions

## Layered Architecture

```
  HTTP Request
       │
       ▼
┌──────────────────────────────────────────────┐
│  Controller Layer                            │
│  [ApiController] [Authorize]                 │
│  Route handling · DTO binding · Auth checks  │
└──────────────────────┬───────────────────────┘
                       │ IService
                       ▼
┌──────────────────────────────────────────────┐
│  Service Layer                               │
│  Business logic · Validation · Orchestration │
└──────────────────────┬───────────────────────┘
                       │ IRepository
                       ▼
┌──────────────────────────────────────────────┐
│  Repository Layer                            │
│  EF Core queries · Exception wrapping        │
└──────────────────────┬───────────────────────┘
                       │
                       ▼
                 ┌───────────┐
                 │ PostgreSQL│
                 └───────────┘
```

- **Controller → Service → Repository** pattern enforces clear separation of concerns
- Each layer has a single responsibility:
  - **Controllers** — HTTP concerns only: routing, request/response mapping, authorization attributes
  - **Services** — business logic, validation, orchestration between repositories
  - **Repositories** — data access, EF Core queries, database exception wrapping
- Layers communicate through **interfaces** (`IInvoiceService`, `IInvoiceRepository`) — never concrete implementations
- This enables unit testing of each layer in isolation via mocking

## Dependency Injection

- All services and repositories registered in `Program.cs` using .NET's built-in DI container
- **Scoped** lifetime for repositories and services — one instance per HTTP request, matching EF Core's `DbContext` lifetime
- **Singleton** for `KafkaProducerService` — Kafka producers are thread-safe and expensive to create; reusing a single instance is the recommended pattern
- Interface-based registration (`AddScoped<IService, Service>()`) enables swapping implementations without touching consumers

## Controller Design

- **`[ApiController]`** attribute enables automatic model validation, binding source inference, and `ProblemDetails` responses
- **`[Authorize]`** at the controller level — all endpoints require authentication by default
- **`AuthenticatedControllerBase`** — abstract base class that extracts user identity (`sub`, `email`, `preferred_username`) from JWT claims
  - Centralizes claim extraction logic — no duplication across controllers
  - Throws `UnauthorizedException` if claims are missing, caught by the global exception handler
- **`[ProducesResponseType]`** attributes on every action — enables accurate OpenAPI spec generation for client code generation

## Exception Handling Strategy

- **Global exception handler** (`GlobalExceptionHandler`) implements `IExceptionHandler` — the .NET 8 recommended approach
- All exceptions map to **RFC 9457 Problem Details** responses — standardized, machine-readable error format
- Custom exception hierarchy lives in the **`Shared.Core`** library (shared with `ManagementApi`):
  - `AppException` (abstract base, `Shared.Core.Exceptions`) — carries `StatusCode`, `Type` (RFC URI), and `Title`
  - Application exceptions (`Shared.Core.Exceptions.Application`): `NotFoundException` (404), `ValidationException` (400), `UnauthorizedException` (401), `ForbiddenException` (403), `ConflictException` (409), `DuplicateEntityException` (409), `BusinessRuleException` (422)
  - Infrastructure exceptions (`Shared.Core.Exceptions.Infrastructure`): `DatabaseUnavailableException` (503), `InfrastructureException` (500)
- **No try/catch in controllers** — exceptions bubble up to the global handler, keeping controllers clean
- Repository layer wraps raw database exceptions (`DbUpdateException`, `DbUpdateConcurrencyException`) into typed application exceptions

## Logging

- **Serilog** with `CompactJsonFormatter` writes structured JSON to stdout — every log line is machine-parseable and enriched with `ServiceName`, `Environment`, `MachineName`, and `ThreadId`
- **OpenTelemetry SDK** provides automatic HTTP request/response spans (`AddAspNetCoreInstrumentation`) and automatically injects W3C `traceparent` headers on all outgoing `HttpClient` calls (`AddHttpClientInstrumentation`)
- **`Serilog.Enrichers.Span`** embeds the active `TraceId` and `SpanId` into every log entry — logs are directly correlated with traces without manual plumbing
- **`KafkaProducerService`** manually injects W3C `traceparent`/`tracestate` headers into each Kafka message before `ProduceAsync` — extends the trace from HTTP into the event-driven layer
- **`GlobalExceptionHandler`** includes `traceId` (from `httpContext.TraceIdentifier`) in all `ProblemDetails` error responses — clients can report the trace ID for support and debugging
- Log levels used intentionally:
  - `Information` — successful operations (entity created, event published)
  - `Warning` — application exceptions (expected errors like not found, validation failures)
  - `Error` — unexpected exceptions, infrastructure failures
- Authentication events (token validation, failures, challenges) logged for security auditing

## Authentication & Authorization

- **JWT Bearer** authentication via Keycloak OIDC
- Token validation: issuer, lifetime, and signing key validated; audience validation relaxed for development flexibility
- **Keycloak realm roles** extracted from the `realm_access` claim in the `OnTokenValidated` event and mapped to standard .NET `ClaimTypes.Role`
- Role-based authorization available via `[Authorize(Roles = "...")]` on controllers/actions
- **`OrganizationAuthorizationFilter`** — globally-applied `IAsyncActionFilter` that closes IDOR vulnerabilities by verifying the authenticated user is a member of the `organizationId` present in each request (query string or route parameter) before the action executes
  - Returns `403 Forbidden` if the user is not a member of the requested organization
  - Opt-out via `[SkipOrgAuth]` attribute for endpoints that intentionally have no organization context
  - Result cached in `HttpContext.Items["OrgAuthPassed"]` per request — the membership check runs at most once even if multiple filter stages fire

## Validation

- **Model validation** via Data Annotations on DTOs — handled automatically by `[ApiController]`
- Custom `InvalidModelStateResponseFactory` returns RFC 9457 Problem Details with field-level error details
- Validation errors include the field name and all associated error messages — frontend can display inline errors

## Repository Pattern

- Generic `IRepository<T>` / `Repository<T>` base provides standard CRUD operations
- Specialized repositories (e.g., `IInvoiceRepository`) extend the base with domain-specific queries (pagination, filtering, includes)
- Repository base wraps all database operations in exception handling:
  - `DbUpdateException` → `InfrastructureException`
  - `DbUpdateConcurrencyException` → `ConflictException` (optimistic concurrency)
  - Generic exceptions → `DatabaseUnavailableException`
- `when (ex is not AppException)` guard prevents double-wrapping of already-typed exceptions

## Workflow Service Architecture

- `WorkflowService` implements the workflow state machine — validates event transitions, mutates status, and records `WorkflowEvent` entries
- **`IWorkflowEventDispatcher`** / `WorkflowEventDispatcher` — separates Kafka publishing from the state machine logic; maps workflow event types to the correct Kafka topic and publishes asynchronously with error isolation (a Kafka failure does not roll back the state transition)
- **`IQuoteToInvoiceConversionService`** / `QuoteToInvoiceConversionService` — isolates the quote-to-invoice conversion step that is triggered by the `ConvertedToInvoice` workflow event
- **`TimeProvider`** injected into services that generate timestamps — improves testability by allowing deterministic time in unit tests

## Kafka Event Production

- `KafkaProducerService` registered as a **singleton** — producer instances are thread-safe and connection pooling is handled internally
- Events are fire-and-forget from the API's perspective — the API does not wait for consumer acknowledgment
- Each event carries a minimal payload (entity ID + timestamp) — consumers fetch full data from the shared database
- `IDisposable` implementation ensures the producer flushes pending messages on shutdown

## API Documentation (OpenAPI / Swagger)

- **NSwag** generates the OpenAPI specification from controller metadata
- Swagger UI available in development for interactive API exploration
- OpenAPI spec consumed by `generate-client.sh` to auto-generate TypeScript API clients for the frontend
- `[ProducesResponseType]` and `[ProducesProblem]` attributes ensure the spec accurately documents all possible response codes

## Pagination

- `PaginatedResponse<T>` DTO provides consistent pagination across all list endpoints
- Server-side pagination with `page` and `pageSize` query parameters
- Prevents unbounded queries — `pageSize` capped at a maximum value

## Mapper Extensions

- Static extension methods (`ToResponse()`, `ToEntity()`) for DTO ↔ Entity mapping
- Chosen over AutoMapper for:
  - Compile-time safety — mapping errors caught at build time, not runtime
  - Transparency — mapping logic is explicit and easy to debug
  - Performance — no reflection overhead
