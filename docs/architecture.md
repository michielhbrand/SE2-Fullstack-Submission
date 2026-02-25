# System Architecture — Design Decisions

## Monorepo Structure

- **Single repository** houses both the Client Application and the Management Application
- Enables shared code (database models, migrations) without publishing internal packages
- Simplifies CI/CD — one pipeline can build, test, and deploy all services
- Clear folder separation (`apps/client`, `apps/management`, `shared`, `infrastructure`) keeps boundaries explicit

## Application Separation

- **Client App** (Invoice Tracker SaaS) and **Management App** (system administration) are fully independent applications
- Each has its own frontend, backend API, and routing — no cross-contamination of concerns
- Separation enforces the **principle of least privilege**: regular users never touch management endpoints; system admins operate through a dedicated portal
- Allows independent scaling — the client app handles high-volume invoice traffic while the management app serves low-frequency admin operations

## Event-Driven Architecture (Kafka)

```
                         ┌─────────────────────┐
                         │  InvoiceTrackerApi  │
                         │     (Producer)      │
                         └──────────┬──────────┘
                                    │ publish
                                    ▼
              ┌─────────────────────────────────────────┐
              │              Apache Kafka               │
              │                                         │
              │  invoice-created  ·  quote-created      │
              │  quote-approval-requested               │
              │  invoice-generated                      │
              └──────┬──────────────────────┬───────────┘
                     │ consume              │ consume
                     ▼                      ▼
          ┌──────────────────┐   ┌─────────────────────┐
          │ PdfGenerator     │   │ EmailNotification   │
          │ Service          │   │ Service             │
          └──────────────────┘   └─────────────────────┘
```

- **Apache Kafka** decouples the main API from downstream processing (PDF generation, email notifications)
- The InvoiceTrackerApi **produces** events; microservices **consume** them asynchronously
- Benefits:
  - API responds immediately — no waiting for PDF rendering or email delivery
  - Fault tolerance — if a microservice is down, events queue in Kafka and are processed on recovery
  - Scalability — consumers can be scaled independently based on load
  - Auditability — Kafka retains event history for replay and debugging
- Topics are domain-specific: `invoice-created`, `quote-created`, `quote-approval-requested`, `invoice-generated`
- **Confluent Kafka** client library chosen for its production-grade .NET support

## Identity & Access Management (Keycloak)

- **Keycloak** provides OpenID Connect (OIDC) / OAuth 2.0 identity management
- Eliminates the need to build custom authentication — password hashing, token issuance, session management, and user federation are handled by a battle-tested solution
- JWT tokens validated by both APIs using standard `JwtBearer` middleware
- Realm roles (`orgUser`, `orgAdmin`, `systemAdmin`) extracted from token claims at the middleware level
- Realm export (`keycloak-realm.json`) enables reproducible development environments with seeded users and roles
- Keycloak Admin API used for user provisioning and role management from the backend

## Database (PostgreSQL)

- **PostgreSQL 15** chosen for:
  - ACID compliance — critical for financial data (invoices, quotes)
  - Rich JSON support for flexible data if needed
  - Mature ecosystem with excellent .NET support via Npgsql / EF Core
- **Shared database** across all services via `Shared.Database` project
  - Single source of truth for the data model
  - EF Core migrations managed centrally — no schema drift between services
  - Code-first approach with strongly-typed models
- Alpine image used in Docker for minimal footprint

## Object Storage (MinIO)

- **MinIO** provides S3-compatible object storage for generated PDFs
- Chosen over filesystem storage because:
  - Stateless services — no local disk dependency
  - Presigned URLs allow secure, time-limited direct downloads without proxying through the API
  - S3 API compatibility means easy migration to AWS S3 or Azure Blob in production
- Bucket initialization handled by a dedicated `BackgroundService` on startup

## Email (MailHog)

- **MailHog** provides a local SMTP server with a web UI for development
- Captures all outgoing emails without actually delivering them — safe for testing
- Drop-in replacement: swap the SMTP config for a real provider (SendGrid, SES) in production with zero code changes
- SMTP protocol chosen over vendor-specific APIs for portability

## Containerized Infrastructure

- **Docker Compose** orchestrates all infrastructure services (Keycloak, PostgreSQL, Kafka, Zookeeper, MinIO, MailHog, Kafka UI)
- Single `docker-compose up -d` brings up the entire development environment
- Named volumes ensure data persistence across container restarts
- Dedicated Docker network (`microservices-network`) isolates service communication
- **Kafka UI** included for development observability — inspect topics, messages, and consumer groups visually

## Multi-Tenancy via Organizations

- Organization-scoped data isolation — all business entities (invoices, quotes, clients, templates) are scoped to an organization
- Users are members of organizations with role-based access (orgUser, orgAdmin)
- Enables SaaS-style multi-tenancy without separate databases per tenant
- Organization context resolved from JWT claims and membership lookups
