# SE2 Fullstack Submission

> 📐 **[Design Decisions](https://michielhbrand.github.io/SE2-Fullstack-Design-Decisions/)** — All architecture and design decisions for this project are documented on a dedicated static site.

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

## Running Locally

### Prerequisites

| Tool | Version |
|---|---|
| Docker & Docker Compose | Latest |
| .NET SDK | 8.0+ |
| Node.js | 18+ |

### Step 1 — Start Infrastructure

```bash
cd infrastructure
docker-compose up -d
```

Wait ~30 seconds for Keycloak to initialize. Verify services are running:

| Service | URL |
|---|---|
| Keycloak | http://localhost:9090 (admin / admin) |
| Kafka UI | http://localhost:8088 |
| MinIO Console | http://localhost:9003 (minioadmin / minioadmin) |
| MailHog | http://localhost:8025 |

### Step 2 — Start Backend Services

Each service runs in its own terminal:

**InvoiceTrackerApi:**

```bash
cd apps/client/backend/InvoiceTrackerApi
dotnet run
```

→ http://localhost:5000 (Swagger: http://localhost:5000/swagger)

**PdfGeneratorService:**

```bash
cd apps/client/backend/PdfGeneratorService
dotnet run
```

→ http://localhost:5001

**EmailNotificationService:**

```bash
cd apps/client/backend/EmailNotificationService
dotnet run
```

→ http://localhost:5003

**ManagementApi:**

```bash
cd apps/management/backend
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

→ http://localhost:5002 (Swagger: http://localhost:5002/swagger)

### Step 3 — Start Frontend Dev Servers

**Client Frontend:**

```bash
cd apps/client/frontend
npm install
npm run dev
```

→ http://localhost:5173

**Management Frontend:**

```bash
cd apps/management/frontend
npm install
npm run dev
```

→ http://localhost:5174

### Step 4 — Login

Use the seeded Keycloak users:

| Username | Password | Role |
|---|---|---|
| `testuser` | `password123` | orgUser |
| `admin` | `admin123` | orgAdmin |
| `system` | `system123` | systemAdmin |
