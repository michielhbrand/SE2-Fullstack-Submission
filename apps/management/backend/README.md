# Management API

Backend API for managing organizations and system administration.

## Version

Current version: **1.0.0** - See [CHANGELOG](docs/CHANGELOG.md) for release history.

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL database
- Keycloak for authentication
- NSwag.ConsoleCore (for client generation)

## Build and Run

Build the project:
```bash
dotnet build
```

Run in development mode:
```bash
dotnet run
```

The API will be available at `http://localhost:5002`

## API Documentation

Swagger UI is available in development mode at:
- `http://localhost:5002/swagger`
- `http://localhost:5002/swagger/v1/swagger.json`

## Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

## TypeScript Client Generation

Install NSwag globally:
```bash
dotnet tool install -g NSwag.ConsoleCore
```

Generate the client:
```bash
./generate-client.sh
```

The generated client will be created at `management/frontend/src/api/generated/api-client.ts`

## Configuration

Configuration is managed through:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- Environment variables

## Project Structure

```
backend/
├── Data/                # Database context
├── DTOs/                # Data transfer objects
├── Endpoints/           # Minimal API endpoints
├── Exceptions/          # Custom exceptions and handlers
├── Extensions/          # Service extensions
├── Mappers/             # Object mappers
├── Migrations/          # EF Core migrations
├── Models/              # Domain models
├── Services/            # Business logic
└── Validators/          # Request validators
```
