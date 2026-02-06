# Minimal FullStack V1 - Monorepo

A monorepo containing two full-stack applications: a **Client/SaaS Application** for invoice tracking and a **Management Application** for organization and user management.

## 📁 Project Structure

```
Minimal-FullStack-V1/
├── apps/
│   ├── client/              # Client/SaaS Application (Invoice Tracker)
│   │   ├── backend/         # .NET 8 API with microservices
│   │   └── frontend/        # Vue.js + TypeScript frontend
│   │
│   └── management/          # Management Application
│       ├── backend/         # .NET 8 Management API
│       └── frontend/        # Vue.js + TypeScript frontend
│
├── infrastructure/          # Shared infrastructure services
│   ├── docker-compose.yml   # Keycloak, PostgreSQL, Kafka, MinIO, etc.
│   └── keycloak-realm.json  # Keycloak realm configuration
│
├── docs/                    # Shared documentation
│   ├── HYBRID_USER_DATA_MODEL.md
│   ├── IMPLEMENTATION_SUMMARY.md
│   ├── INVOICE_TRACKER_USER_DIRECTORY.md
│   ├── KEYCLOAK_EVENT_SYNC.md
│   └── PDF_GENERATION_SYSTEM.md
│
└── README.md               # This file
```

## 🚀 Applications

### Client Application (Invoice Tracker)

A SaaS application for managing invoices, quotes, clients, and templates.

**Backend** (`apps/client/backend/`)
- **InvoiceTrackerApi**: Main API service (Port: 5000)
- **PdfGeneratorService**: Microservice for PDF generation
- **Tech Stack**: .NET 8, Entity Framework Core, PostgreSQL, Kafka, MinIO

**Frontend** (`apps/client/frontend/`)
- **Tech Stack**: Vue.js 3, TypeScript, Vite, Tailwind CSS, Vuetify
- **Port**: 5173 (dev server)

### Management Application

An administrative application for managing organizations and users.

**Backend** (`apps/management/backend/`)
- **ManagementApi**: Organization and user management API (Port: 5002)
- **Tech Stack**: .NET 8, Entity Framework Core, PostgreSQL, Keycloak

**Frontend** (`apps/management/frontend/`)
- **Tech Stack**: Vue.js 3, TypeScript, Vite, Tailwind CSS
- **Port**: 5174 (dev server)

## 🛠️ Infrastructure Services

The `infrastructure/` directory contains Docker Compose configuration for shared services:

- **Keycloak** (Port: 9090) - Authentication & authorization
- **PostgreSQL** - Multiple databases for different services
- **Kafka** (Port: 9093) - Event streaming
- **Zookeeper** (Port: 2181) - Kafka coordination
- **MinIO** (Ports: 9002, 9003) - Object storage for PDFs
- **Kafka UI** (Port: 8088) - Kafka management interface

### Starting Infrastructure

```bash
cd infrastructure
docker-compose up -d
```

## 📦 Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- Docker and Docker Compose
- NSwag CLI (for API client generation): `dotnet tool install -g NSwag.ConsoleCore`

### Client Application Setup

**Backend:**
```bash
cd apps/client/backend/InvoiceTrackerApi
dotnet restore
dotnet run
```

**Frontend:**
```bash
cd apps/client/frontend
npm install
npm run dev
```

### Management Application Setup

**Backend:**
```bash
cd apps/management/backend
dotnet restore
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

**Frontend:**
```bash
cd apps/management/frontend
npm install
npm run dev
```

## 🔄 API Client Generation

Both applications use NSwag to generate TypeScript API clients from OpenAPI specifications.

**Client Application:**
```bash
cd apps/client/backend/InvoiceTrackerApi
./generate-client.sh
```

**Management Application:**
```bash
cd apps/management/backend
./generate-client.sh
```

## 🗄️ Database Migrations

**Client Application:**
```bash
cd apps/client/backend/InvoiceTrackerApi
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

**Management Application:**
```bash
cd apps/management/backend
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## 🔐 Authentication

Both applications use Keycloak for authentication:
- **Admin Console**: http://localhost:9090
- **Default Credentials**: admin / admin
- **Realm Configuration**: `infrastructure/keycloak-realm.json`

## 📚 Documentation

Detailed documentation is available in the `docs/` directory:

- **HYBRID_USER_DATA_MODEL.md** - User data model architecture
- **IMPLEMENTATION_SUMMARY.md** - Implementation overview
- **INVOICE_TRACKER_USER_DIRECTORY.md** - User directory service
- **KEYCLOAK_EVENT_SYNC.md** - Keycloak event synchronization
- **PDF_GENERATION_SYSTEM.md** - PDF generation architecture

## 🏗️ Architecture

This monorepo follows an **Apps-First Organization** pattern:

- **Clear Boundaries**: Each application is self-contained under `apps/`
- **Consistent Structure**: Both apps follow identical organizational patterns
- **Shared Resources**: Infrastructure and documentation are centralized
- **Independent Deployment**: Each app can be deployed independently
- **Scalable**: Easy to add new applications to the monorepo

## 🧪 Development Workflow

1. Start infrastructure services: `cd infrastructure && docker-compose up -d`
2. Start backend services for the app you're working on
3. Start frontend dev server for the app you're working on
4. Make changes and test
5. Generate API clients when backend changes: `./generate-client.sh`
6. Commit changes

## 📝 Environment Variables

**Client Frontend** (`apps/client/frontend/.env`):
```
VITE_API_URL=http://localhost:5000
```

**Management Frontend** (`apps/management/frontend/.env`):
```
VITE_API_URL=http://localhost:5002
```

## 🤝 Contributing

When adding new features:
1. Keep application-specific code within the respective `apps/` directory
2. Place shared infrastructure in `infrastructure/`
3. Document architectural decisions in `docs/`
4. Update this README when adding new services or applications

## 📄 License

[Add your license information here]
