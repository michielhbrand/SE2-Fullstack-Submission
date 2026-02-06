# Management API

Backend API for managing organizations and system administration.

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL database
- Keycloak for authentication
- NSwag.ConsoleCore (for client generation)

## Running the API

### Development Mode

To run the API in development mode (with Swagger/OpenAPI enabled):

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Or simply:

```bash
dotnet run
```

The API will be available at `http://localhost:5002`

### Swagger UI

When running in Development mode, Swagger UI is available at:
- Swagger UI: `http://localhost:5002/swagger`
- OpenAPI Spec: `http://localhost:5002/swagger/v1/swagger.json`

## Generating TypeScript Client

The TypeScript client is automatically generated from the OpenAPI specification using NSwag.

### Prerequisites

Install NSwag globally:

```bash
dotnet tool install -g NSwag.ConsoleCore
```

### Generate Client

1. Ensure the Management API is running in Development mode
2. Run the generation script:

```bash
cd management/backend
./generate-client.sh
```

The generated TypeScript client will be created at:
`management/frontend/src/api/generated/api-client.ts`

### Troubleshooting

If you get a 404 error when generating the client:

1. Make sure the API is running: `curl http://localhost:5002/health`
2. Verify OpenAPI is available: `curl http://localhost:5002/swagger/v1/swagger.json`
3. If OpenAPI returns 404, ensure the API is running in Development mode:
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run
   ```

## Configuration

Configuration is managed through:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- Environment variables

### Key Configuration

- **Database**: Connection string in `appsettings.json`
- **Keycloak**: Authentication settings in `appsettings.json`
- **CORS**: Configured for the management portal frontend

## API Endpoints

### Health Check
- `GET /health` - Health check endpoint

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration

### Organizations
- `GET /api/organizations` - List organizations
- `POST /api/organizations` - Create organization
- `GET /api/organizations/{id}` - Get organization details
- `PUT /api/organizations/{id}` - Update organization
- `DELETE /api/organizations/{id}` - Delete organization

## Development

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

### Project Structure

```
management/backend/
├── Controllers/          # API endpoints
├── Data/                # Database context
├── DTOs/                # Data transfer objects
├── Endpoints/           # Minimal API endpoints
├── Exceptions/          # Custom exceptions
├── Extensions/          # Service extensions
├── Models/              # Domain models
├── Repositories/        # Data access layer
├── Services/            # Business logic
└── Properties/          # Launch settings
```
