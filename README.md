# SE2 Fullstack Submission

> 📐 <strong><a href="https://michielhbrand.github.io/SE2-Fullstack-Design-Decisions/" target="_blank" rel="noopener noreferrer">
> Design Decisions
> </a></strong> — All architecture and design decisions for this project are documented on a dedicated static site.

## Running Locally

### Prerequisites

| Tool | Version |
|---|---|
| Docker & Docker Compose | Latest |

### Start Everything

From the repository root, run:

```bash
docker-compose up --build
```

This builds and starts all 14 services — infrastructure, backends, and frontends — in a single step. The first build takes a few minutes. Subsequent starts (without `--build`) are fast.

> **Wait ~60 seconds** after startup for Keycloak to fully initialise before logging in.

### Service URLs

| Service | URL | Notes |
|---|---|---|
| Client App | http://localhost:5173 | Main invoice tracker |
| Management App | http://localhost:5174 | Org & user admin |
| InvoiceTrackerApi | http://localhost:5000/swagger | API docs |
| ManagementApi | http://localhost:5002/swagger | API docs |
| Keycloak | http://localhost:9090 | admin / admin |
| MailHog | http://localhost:8025 | Captured emails |
| MinIO Console | http://localhost:9003 | minioadmin / minioadmin |
| Kafka UI | http://localhost:8088 | Kafka topics |

### Login

| Username | Password | Role |
|---|---|---|
| `testuser` | `password123` | orgUser |
| `admin` | `admin123` | orgAdmin |
| `system` | `system123` | systemAdmin |

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            FRONTENDS                                    │
│                                                                         │
│   Client Frontend (Vue 3 + Vite) :5173                                  │
│   Management Frontend (Vue 3 + Vite) :5174                              │
└──────────────┬──────────────────────────────┬───────────────────────────┘
               │ REST                         │ REST
               ▼                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           BACKEND APIs                                  │
│                                                                         │
│   InvoiceTrackerApi (.NET 8) :5000          ManagementApi (.NET 8) :5002│
└──────┬──────────┬──────────┬────────────────────┬──────────┬────────────┘
       │          │          │                    │          │
       │ OIDC/JWT │ EF Core  │ Produce Events     │ OIDC/JWT │ EF Core
       ▼          ▼          ▼                    ▼          ▼
┌───────────┐ ┌────────┐ ┌────────┐        ┌───────────┐ ┌────────┐
│ Keycloak  │ │Postgres│ │ Kafka  │        │ Keycloak  │ │Postgres│
│   :9090   │ │ :5433  │ │ :9093  │        │  (shared) │ │(shared)│
└───────────┘ └────────┘ └───┬────┘        └───────────┘ └────────┘
                             │ Consume Events
                    ┌────────┴─────────┐
                    ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          MICROSERVICES                                  │
│                                                                         │
│   PdfGeneratorService (.NET 8) :5001                                    │
│     ├── EF Core ──► PostgreSQL                                          │
│     └── Store PDFs ──► MinIO :9002                                      │
│                                                                         │
│   EmailNotificationService (.NET 8) :5003                               │
│     ├── EF Core ──► PostgreSQL                                          │
│     ├── Retrieve PDFs ──► MinIO                                         │
│     └── SMTP ──► MailHog :1025 / :8025                                  │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                         INFRASTRUCTURE                                  │
│                                                                         │
│   Keycloak :9090          PostgreSQL :5433        Kafka :9093           │
│   MinIO :9002             MailHog :1025/:8025     Kafka UI :8088        │
└─────────────────────────────────────────────────────────────────────────┘
```

## System Description

This is a monorepo containing two full-stack applications: a **Client Application** (Invoice Tracker SaaS) and a **Management Application** (organization & user administration). Both share a common PostgreSQL database and Keycloak identity provider.

The Client Application backend (InvoiceTrackerApi) is the main API handling invoices, quotes, clients, templates, workflows, and organizations. It produces Kafka events consumed by two microservices: **PdfGeneratorService** (generates PDFs from HTML templates and stores them in MinIO) and **EmailNotificationService** (sends email notifications via SMTP with PDF attachments).

The Management Application backend (ManagementApi) provides system-wide organization and user management with rate limiting. Both frontends are built with Vue 3, TypeScript, Vite, and Tailwind CSS, communicating with their respective APIs via auto-generated TypeScript clients (NSwag).

## Future Improvements

The following improvements are out of scope for the current submission but are planned for future iterations:

- **OpenTelemetry Collector** — receive OTLP traces and logs over gRPC from all four services and forward to configurable backends
- **Grafana Tempo** — distributed trace storage and visualization; correlate spans across `InvoiceTrackerApi`, `PdfGeneratorService`, and `EmailNotificationService` in a single waterfall view
- **Grafana Loki** — centralized log aggregation; query logs by `TraceId` and `ServiceName` across all services
- **Grafana dashboard** — unified view linking trace spans to their correlated log lines (Tempo ↔ Loki correlation)
- **Prometheus metrics endpoints** — expose `/metrics` per service and build Grafana dashboards for request rate, error rate, and duration (RED method)
- **Enhanced health checks** — dependency-aware health checks for Kafka, MinIO, and PostgreSQL so that `/health` reports the actual reachability of each downstream dependency
- **Organization-level template management** — allow each organization to create, upload, and manage their own invoice and quote HTML templates; currently all organizations share the same system-wide templates, with the infrastructure (storage keys, type scoping, and per-org uniqueness constraints) already in place to support this
