# Asynchronous PDF Generation System

## Overview

This system implements asynchronous PDF generation for invoices using a microservices architecture with Kafka message broker and MinIO object storage.

## Architecture

```
┌─────────────┐      ┌──────────┐      ┌────────────────────┐      ┌────────┐
│   Frontend  │─────▶│ AuthApi  │─────▶│  Kafka (invoice-   │─────▶│ MinIO  │
│     (UI)    │      │          │      │  created topic)    │      │Storage │
└─────────────┘      └──────────┘      └────────────────────┘      └────────┘
                           │                      │                      ▲
                           │                      ▼                      │
                           │            ┌──────────────────┐            │
                           │            │ PdfGenerator     │────────────┘
                           │            │ Service          │
                           │            └──────────────────┘
                           │                      │
                           ▼                      ▼
                     ┌──────────────────────────────┐
                     │      PostgreSQL Database      │
                     └──────────────────────────────┘
```

## Workflow

1. **Invoice Creation**: User creates an invoice through the UI
2. **Database Storage**: AuthApi stores invoice details in PostgreSQL
3. **Event Publishing**: AuthApi publishes an `invoice-created` event to Kafka
4. **Event Consumption**: PdfGeneratorService consumes the event
5. **PDF Generation**: Service generates PDF from HTML template using PuppeteerSharp
6. **Storage**: PDF is uploaded to MinIO object storage
7. **Database Update**: Invoice record is updated with the PDF storage key

## Components

### 1. AuthApi (Port 5000)
- Main API service handling invoice CRUD operations
- Publishes Kafka events when invoices are created
- **New Files**:
  - [`Services/IKafkaProducerService.cs`](backend/AuthApi/Services/IKafkaProducerService.cs)
  - [`Services/KafkaProducerService.cs`](backend/AuthApi/Services/KafkaProducerService.cs)
- **Modified Files**:
  - [`Models/Invoice.cs`](backend/AuthApi/Models/Invoice.cs) - Added `PdfStorageKey` field
  - [`Controllers/InvoiceController.cs`](backend/AuthApi/Controllers/InvoiceController.cs) - Integrated Kafka producer
  - [`Program.cs`](backend/AuthApi/Program.cs) - Registered Kafka service
  - [`appsettings.json`](backend/AuthApi/appsettings.json) - Added Kafka configuration

### 2. PdfGeneratorService (Port 5001)
- Microservice dedicated to PDF generation
- Consumes Kafka events and processes invoices asynchronously
- **Key Files**:
  - [`BackgroundServices/InvoiceCreatedConsumer.cs`](backend/PdfGeneratorService/BackgroundServices/InvoiceCreatedConsumer.cs) - Kafka consumer
  - [`Services/PdfGenerationService.cs`](backend/PdfGeneratorService/Services/PdfGenerationService.cs) - PDF generation logic
  - [`Services/MinioStorageService.cs`](backend/PdfGeneratorService/Services/MinioStorageService.cs) - MinIO integration
  - [`Templates/InvoiceTemplate.html`](backend/PdfGeneratorService/Templates/InvoiceTemplate.html) - HTML template
  - [`Models/Invoice.cs`](backend/PdfGeneratorService/Models/Invoice.cs) - Invoice model
  - [`Models/InvoiceItem.cs`](backend/PdfGeneratorService/Models/InvoiceItem.cs) - Invoice item model
  - [`Data/ApplicationDbContext.cs`](backend/PdfGeneratorService/Data/ApplicationDbContext.cs) - Database context
  - [`Program.cs`](backend/PdfGeneratorService/Program.cs) - Service configuration
  - [`appsettings.json`](backend/PdfGeneratorService/appsettings.json) - Configuration

### 3. Infrastructure Services
- **Kafka** (Port 9092): Message broker for event-driven communication
- **Zookeeper** (Port 2181): Kafka coordination service
- **MinIO** (Port 9000, Console 9001): S3-compatible object storage
- **PostgreSQL** (Port 5432): Shared database for both services

## Setup Instructions

### 1. Start Infrastructure Services

```bash
cd infrastructure
docker-compose up -d
```

This starts:
- Keycloak (authentication)
- PostgreSQL (database)
- Kafka + Zookeeper (message broker)
- MinIO (object storage)

### 2. Apply Database Migrations

```bash
cd backend/AuthApi
dotnet ef database update
```

### 3. Start AuthApi

```bash
cd backend/AuthApi
dotnet run
```

The API will be available at `http://localhost:5000`

### 4. Start PdfGeneratorService

```bash
cd backend/PdfGeneratorService
dotnet run
```

The service will be available at `http://localhost:5001`

### 5. Start Frontend

```bash
cd frontend/app
npm install
npm run dev
```

The UI will be available at `http://localhost:5173`

## Configuration

### AuthApi Configuration ([`appsettings.json`](backend/AuthApi/appsettings.json))

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

### PdfGeneratorService Configuration ([`appsettings.json`](backend/PdfGeneratorService/appsettings.json))

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  }
}
```

## Testing the System

### 1. Create an Invoice via UI
- Navigate to the Invoices page
- Click "New Invoice"
- Fill in client details and invoice items
- Submit the form

### 2. Monitor the Process

**Check AuthApi logs:**
```
Invoice {InvoiceId} created by {User}
Invoice created event published to Kafka...
```

**Check PdfGeneratorService logs:**
```
Received message from Kafka...
Processing invoice created event for InvoiceId: {InvoiceId}
Generating PDF for Invoice {InvoiceId}
PDF generated successfully...
PDF uploaded to MinIO...
Successfully processed invoice {InvoiceId}. PDF stored at: {StorageKey}
```

### 3. Verify PDF Storage

**Access MinIO Console:**
- URL: `http://localhost:9001`
- Username: `minioadmin`
- Password: `minioadmin`
- Navigate to the `invoices` bucket to see generated PDFs

### 4. Check Database

```sql
SELECT Id, ClientName, ClientSurname, PdfStorageKey, DateCreated 
FROM "Invoices" 
WHERE PdfStorageKey IS NOT NULL;
```

## API Endpoints

### AuthApi

- `POST /api/Invoice` - Create new invoice (triggers PDF generation)
- `GET /api/Invoice` - List all invoices
- `GET /api/Invoice/{id}` - Get invoice by ID
- `PUT /api/Invoice/{id}` - Update invoice
- `DELETE /api/Invoice/{id}` - Delete invoice
- `GET /health` - Health check

### PdfGeneratorService

- `GET /health` - Health check

## Kafka Topics

- **invoice-created**: Published when a new invoice is created
  - Key: Invoice ID (string)
  - Value: JSON with `InvoiceId` and `Timestamp`

## MinIO Buckets

- **invoices**: Stores generated PDF files
  - Object naming: `invoice-{invoiceId}-{timestamp}.pdf`

## Database Schema Changes

### Invoice Table
Added new column:
- `PdfStorageKey` (varchar(500), nullable): Stores the MinIO object path

## Dependencies

### AuthApi
- `Confluent.Kafka` (2.13.0) - Kafka producer

### PdfGeneratorService
- `Confluent.Kafka` (2.13.0) - Kafka consumer
- `Minio` (7.0.0) - MinIO client
- `PuppeteerSharp` (20.2.5) - PDF generation from HTML
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.0) - PostgreSQL provider

## Troubleshooting

### Kafka Connection Issues
- Ensure Kafka and Zookeeper containers are running: `docker ps`
- Check Kafka logs: `docker logs kafka`
- Verify bootstrap servers configuration in appsettings.json

### MinIO Connection Issues
- Verify MinIO container is running
- Check MinIO console at `http://localhost:9001`
- Ensure credentials match in appsettings.json

### PDF Generation Issues
- PuppeteerSharp downloads Chromium on first run (may take time)
- Check PdfGeneratorService logs for detailed error messages
- Ensure sufficient disk space for Chromium and generated PDFs

### Database Issues
- Verify PostgreSQL container is running
- Run migrations: `dotnet ef database update`
- Check connection string in appsettings.json

## Future Enhancements

1. **Retry Mechanism**: Implement retry logic for failed PDF generations
2. **Dead Letter Queue**: Handle permanently failed messages
3. **PDF Download Endpoint**: Add API endpoint to download generated PDFs
4. **Email Notifications**: Send email with PDF attachment when generation completes
5. **Template Customization**: Allow users to customize invoice templates
6. **Batch Processing**: Support bulk invoice PDF generation
7. **Monitoring**: Add metrics and monitoring (Prometheus, Grafana)
8. **Caching**: Cache frequently accessed PDFs

## Security Considerations

1. **MinIO Access**: In production, use secure credentials and enable HTTPS
2. **Kafka Security**: Enable SASL/SSL for Kafka in production
3. **Database**: Use connection pooling and parameterized queries
4. **API Authentication**: Ensure all endpoints are properly authenticated
5. **File Storage**: Implement access control for stored PDFs

## Performance Considerations

1. **Async Processing**: PDF generation doesn't block invoice creation
2. **Scalability**: Multiple PdfGeneratorService instances can consume from Kafka
3. **Resource Management**: PuppeteerSharp browser instances are properly disposed
4. **Database Connections**: Use connection pooling for optimal performance

## License

This project is part of the Minimal-FullStack-V1 application.
