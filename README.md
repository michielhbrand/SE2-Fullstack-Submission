# Minimal Full-Stack Microservices Project

A complete full-stack microservices application with Vue.js frontend, .NET Web API backend, Keycloak authentication, and asynchronous PDF generation using Kafka and MinIO, designed to run locally on macOS using Rancher.

## 🏗️ Architecture

### Frontend
- **Framework**: Vue.js 3 with TypeScript
- **Build Tool**: Vite
- **UI Components**: shadcn-vue with Tailwind CSS
- **Routing**: Vue Router
- **HTTP Client**: Axios
- **Authentication**: OAuth2.0 via Keycloak

### Backend Services
- **AuthApi**: Main .NET 8.0 Web API
  - JWT Bearer authentication with Keycloak
  - Invoice management (CRUD operations)
  - Kafka event producer
  - API Documentation: Scalar
  - Health Checks: Built-in health endpoint

- **PdfGeneratorService**: Dedicated PDF generation microservice
  - Kafka event consumer
  - HTML to PDF conversion using PuppeteerSharp
  - MinIO object storage integration
  - Asynchronous processing

### Identity Service
- **Provider**: Keycloak (containerized)
- **Port**: 9090 (custom, not default 9000)
- **Realm**: microservices
- **Protocol**: OAuth2.0 / OpenID Connect

### Message Broker & Storage
- **Message Broker**: Apache Kafka with Zookeeper
- **Object Storage**: MinIO (S3-compatible)
- **Database**: PostgreSQL (shared by AuthApi and PdfGeneratorService)

### Infrastructure
- **Container Runtime**: Rancher Desktop (instead of Docker Desktop)
- **Orchestration**: Docker Compose

## 📁 Project Structure

```
Minimal-FullStack-V1/
├── frontend/
│   └── app/                    # Vue.js application
│       ├── src/
│       │   ├── components/     # UI components
│       │   │   └── ui/         # shadcn-vue components
│       │   ├── views/          # Page components
│       │   │   ├── Login.vue
│       │   │   ├── Welcome.vue
│       │   │   ├── Invoices.vue
│       │   │   └── Clients.vue
│       │   ├── router/         # Vue Router configuration
│       │   ├── services/       # API services
│       │   └── lib/            # Utility functions
│       └── ...
├── backend/
│   ├── AuthApi/                # Main .NET Web API
│   │   ├── Controllers/        # API controllers
│   │   ├── Services/           # Business services (Kafka producer)
│   │   ├── Models/             # Data models
│   │   ├── Data/               # Database context
│   │   ├── Program.cs          # Main application configuration
│   │   └── appsettings.json    # Configuration settings
│   └── PdfGeneratorService/    # PDF generation microservice
│       ├── BackgroundServices/ # Kafka consumer
│       ├── Services/           # PDF & MinIO services
│       ├── Models/             # Data models
│       ├── Data/               # Database context
│       ├── Templates/          # HTML invoice template
│       ├── Program.cs          # Service configuration
│       └── appsettings.json    # Configuration settings
├── infrastructure/
│   ├── docker-compose.yml      # Container orchestration (Keycloak, Kafka, MinIO, PostgreSQL)
│   └── keycloak-realm.json     # Keycloak realm configuration
├── docs/
│   └── PDF_GENERATION_SYSTEM.md # PDF generation system documentation
└── README.md
```

## 🚀 Prerequisites

### Required Software
1. **Rancher Desktop** - Container runtime for macOS
   - Download from: https://rancherdesktop.io/
   - Configure to use `dockerd (moby)` as container runtime

2. **.NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0

3. **Node.js** (v18 or higher)
   - Download from: https://nodejs.org/

## 📦 Installation & Setup

### 1. Start Infrastructure Services

```bash
cd infrastructure
docker-compose up -d
```

This will start:
- Keycloak (authentication) on port 9090
- PostgreSQL (database) on port 5432
- Kafka (message broker) on port 9092
- Zookeeper (Kafka coordination) on port 2181
- MinIO (object storage) on ports 9000 (API) and 9001 (Console)

Wait for all services to start (approximately 30-60 seconds). Verify they're running:
```bash
curl http://localhost:9090/health  # Keycloak
curl http://localhost:9001          # MinIO Console
```

### 2. Configure Keycloak Realm

1. Access Keycloak Admin Console: http://localhost:9090
2. Login with credentials:
   - Username: `admin`
   - Password: `admin`
3. Import the realm configuration:
   - Click on the realm dropdown (top left)
   - Select "Create Realm"
   - Click "Browse" and select [`infrastructure/keycloak-realm.json`](infrastructure/keycloak-realm.json)
   - Click "Create"

### 3. Apply Database Migrations

```bash
cd backend/AuthApi
dotnet ef database update
```

### 4. Start Backend Services

**Start AuthApi (Main API):**
```bash
cd backend/AuthApi
dotnet restore
dotnet run
```

The API will start on `http://localhost:5000`

**API Endpoints:**
- Health Check: `http://localhost:5000/health`
- API Documentation: `http://localhost:5000/scalar/v1`
- Invoices: `http://localhost:5000/api/Invoice`
- Clients: `http://localhost:5000/api/Client`
- User Info (protected): `http://localhost:5000/api/user`

**Start PdfGeneratorService (in a new terminal):**
```bash
cd backend/PdfGeneratorService
dotnet restore
dotnet run
```

The service will start on `http://localhost:5001`

**Service Endpoints:**
- Health Check: `http://localhost:5001/health`

### 5. Start Frontend Application

```bash
cd frontend/app
npm install
npm run dev
```

The frontend will start on `http://localhost:5173`

## 🔐 Authentication

### Test Credentials

The application comes with a pre-configured test user:

- **Username**: `testuser`
- **Password**: `password123`

### OAuth2.0 Flow

1. User enters credentials on login page
2. Frontend sends credentials to Keycloak token endpoint
3. Keycloak validates and returns JWT access token
4. Frontend stores token in localStorage
5. Frontend includes token in Authorization header for API requests
6. Backend validates token with Keycloak
7. Backend returns user information

## 🎯 Features

### Frontend Features
- ✅ Modern UI with shadcn-vue components
- ✅ Responsive design with Tailwind CSS
- ✅ Protected routes with Vue Router
- ✅ OAuth2.0 authentication flow
- ✅ Token-based API communication
- ✅ Invoice management (CRUD operations)
- ✅ Client management
- ✅ Login and Welcome pages

### Backend Features
- ✅ JWT Bearer authentication
- ✅ Keycloak integration
- ✅ CORS configuration for frontend
- ✅ Health check endpoints
- ✅ Scalar API documentation
- ✅ Invoice and Client APIs
- ✅ **Asynchronous PDF generation**
- ✅ **Kafka event-driven architecture**
- ✅ **MinIO object storage integration**
- ✅ PostgreSQL database with EF Core

### Infrastructure Features
- ✅ Containerized services (Keycloak, Kafka, MinIO, PostgreSQL)
- ✅ Apache Kafka message broker
- ✅ MinIO S3-compatible object storage
- ✅ Zookeeper for Kafka coordination
- ✅ Pre-configured realm and client
- ✅ Test user setup
- ✅ Rancher Desktop compatible

### PDF Generation System
- ✅ **Event-driven architecture** - Kafka-based async processing
- ✅ **HTML to PDF conversion** - Using PuppeteerSharp/Chromium
- ✅ **Object storage** - PDFs stored in MinIO
- ✅ **Database tracking** - PDF storage keys in Invoice records
- ✅ **Microservice architecture** - Dedicated PdfGeneratorService
- ✅ **Professional templates** - Customizable HTML invoice templates

📖 **[View detailed PDF Generation System documentation](docs/PDF_GENERATION_SYSTEM.md)**

## 🔧 Configuration

### Frontend Configuration

Edit [`frontend/app/.env`](frontend/app/.env):
```env
VITE_API_URL=http://localhost:5000
VITE_KEYCLOAK_URL=http://localhost:9090
VITE_KEYCLOAK_REALM=microservices
VITE_KEYCLOAK_CLIENT_ID=frontend-app
```

### Backend Configuration

Edit [`backend/AuthApi/appsettings.json`](backend/AuthApi/appsettings.json):
```json
{
  "Keycloak": {
    "Authority": "http://localhost:9090/realms/microservices",
    "Audience": "backend-api"
  }
}
```

### Keycloak Configuration

The realm configuration is in [`infrastructure/keycloak-realm.json`](infrastructure/keycloak-realm.json). Key settings:

- **Realm**: microservices
- **Clients**:
  - `frontend-app` (public client for Vue.js)
  - `backend-api` (bearer-only for .NET API)
- **Users**: testuser with password123
- **Redirect URIs**: http://localhost:5173/*, http://localhost:3000/*

## 🧪 Testing the Application

### 1. Test Health Endpoints

```bash
# Backend health
curl http://localhost:5000/health

# Keycloak health
curl http://localhost:9090/health
```

### 2. Test Authentication Flow

1. Open browser to `http://localhost:5173`
2. You should see the login page
3. Enter test credentials:
   - Username: `testuser`
   - Password: `password123`
4. Click "Sign In"
5. You should be redirected to the Welcome page showing "Welcome testuser!"

### 3. Test API Endpoints

```bash
# Public endpoint (no auth required)
curl http://localhost:5000/api/public

# Get access token
TOKEN=$(curl -X POST "http://localhost:9090/realms/microservices/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=frontend-app" \
  -d "grant_type=password" \
  -d "username=testuser" \
  -d "password=password123" \
  | jq -r '.access_token')

# Protected endpoint (requires auth)
curl http://localhost:5000/api/user \
  -H "Authorization: Bearer $TOKEN"
```

## 📚 API Documentation

Access the Scalar API documentation at: `http://localhost:5000/scalar/v1`

Scalar provides a modern, interactive API documentation interface as an alternative to Swagger UI.

## 🛑 Stopping the Application

### Stop Frontend
Press `Ctrl+C` in the terminal running the frontend

### Stop Backend
Press `Ctrl+C` in the terminal running the backend

### Stop Keycloak
```bash
cd infrastructure
docker-compose down
```

To remove volumes as well:
```bash
docker-compose down -v
```

## 🔄 Development Workflow

### Making Changes

**Frontend:**
- Hot reload is enabled - changes reflect immediately
- Edit files in [`frontend/app/src/`](frontend/app/src/)

**Backend:**
- Restart required after code changes
- Use `dotnet watch run` for auto-restart on changes

**Keycloak:**
- Changes via admin console persist in Docker volume
- To reset: `docker-compose down -v && docker-compose up -d`

## 📝 Additional Notes

### Port Configuration
- Frontend: 5173 (Vite default)
- AuthApi: 5000 (.NET default)
- PdfGeneratorService: 5001
- Keycloak: 9090 (custom, not default 9000)
- PostgreSQL: 5432
- Kafka: 9092
- Zookeeper: 2181
- MinIO API: 9000
- MinIO Console: 9001

### Security Considerations
- This is a development setup - not production-ready
- HTTPS is disabled for local development
- Default admin credentials should be changed
- Token validation is configured for development

### Rancher Desktop vs Docker Desktop
This project uses Rancher Desktop as specified. If you need to use Docker Desktop:
- The [`docker-compose.yml`](infrastructure/docker-compose.yml) file is compatible
- No changes needed to configuration files