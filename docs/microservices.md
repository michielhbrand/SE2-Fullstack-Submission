# Microservices — Design Decisions

## PdfGeneratorService

### PDF Generation Flow

```
  Kafka Topic                Shared DB            HTML Template
  (invoice-created)          (PostgreSQL)         (InvoiceTemplate.html)
       │                         │                       │
       │ consume                 │ fetch entity          │ load
       ▼                         ▼                       ▼
┌──────────────────────────────────────────────────────────────┐
│                    PdfGeneratorService                       │
│                                                              │
│  BackgroundService ──► fetch data ──► render HTML ──► PDF    │
└──────────────────────────────┬───────────────────────────────┘
                               │ upload
                               ▼
                        ┌─────────────┐
                        │    MinIO    │
                        │  (S3 Bucket)│
                        └─────────────┘
```

### Purpose & Responsibility

- Single responsibility: consume Kafka events and generate PDF documents from HTML templates
- Fully decoupled from the main API — communicates only via Kafka events and the shared database
- Stateless — all generated PDFs stored in MinIO, not on local disk

### Kafka Consumer Pattern

- Implemented as **BackgroundServices** (`InvoiceCreatedConsumer`, `QuoteCreatedConsumer`) — .NET's built-in hosted service pattern
- Each consumer runs as a long-lived background task with its own Kafka consumer group
- Benefits:
  - Automatic lifecycle management — starts with the application, gracefully shuts down
  - Independent consumption — invoice and quote events processed by separate consumers
  - Consumer group isolation — each consumer tracks its own offsets

### HTML Template-Based PDF Generation

- PDFs generated from **HTML templates** (`InvoiceTemplate.html`, `QuoteTemplate.html`)
- Benefits over code-based PDF generation:
  - Templates are editable by non-developers — HTML/CSS skills are sufficient
  - Visual fidelity — what you see in a browser is what you get in the PDF
  - Rapid iteration — change the template, regenerate, no recompilation needed
- Template data populated by fetching the full entity from the shared database using the event's entity ID

### MinIO Storage

- Generated PDFs uploaded to MinIO immediately after generation
- `MinioInitializationService` (BackgroundService) ensures the storage bucket exists on startup — no manual setup required
- S3-compatible API means the same code works with AWS S3 in production

### Service Architecture

- `IPdfGenerationService` — renders HTML templates to PDF
- `IMinioStorageService` — handles upload/download/presigned URL generation
- Scoped services created per-event via `IServiceScopeFactory` — ensures fresh `DbContext` per event processing

---

## EmailNotificationService

### Purpose & Responsibility

- Single responsibility: consume Kafka events and send email notifications with PDF attachments
- Handles two event types:
  - `quote-approval-requested` — sends quote approval emails to clients with a link to approve/reject
  - `invoice-generated` — sends invoice emails to clients with the PDF attached

### Kafka Consumer Pattern

- Same **BackgroundService** pattern as PdfGeneratorService
- `QuoteApprovalRequestedConsumer` and `InvoiceGeneratedConsumer` run as independent consumers
- Each consumer has its own consumer group for independent offset tracking

### Email Service

- `IEmailService` abstracts email sending — SMTP configuration injected via `appsettings.json`
- `EmailTemplates` class provides HTML email templates — professional, branded email content
- SMTP protocol used for maximum portability — works with MailHog in development, any SMTP provider in production

### Token-Based Quote Approval

- `ITokenService` generates secure, time-limited tokens for quote approval links
- Tokens embedded in email links — clients can approve/reject quotes without logging in
- `QuoteResponseController` handles the approval/rejection via token validation
- Security: tokens are single-use and expire after a configured duration

### PDF Attachment Workflow

- For invoice emails: fetches the generated PDF from MinIO and attaches it to the email
- Decoupled from PDF generation — the email service waits for the `invoice-generated` event, which fires only after the PDF is successfully stored

---

## Shared Design Patterns Across Microservices

### Shared Database (Shared.Database)

- Both microservices reference the `Shared.Database` project for `ApplicationDbContext` and entity models
- Ensures schema consistency — all services read/write the same tables with the same model definitions
- EF Core migrations managed centrally — microservices consume the schema, they don't own it

### Health Endpoints

- Both services expose a `/health` endpoint — enables container orchestrators and monitoring tools to verify service availability
- Lightweight implementation — returns service name and status without database checks

### Logging

- **Serilog** with `CompactJsonFormatter` writes structured JSON to stdout — every log line is enriched with `ServiceName`, `Environment`, `MachineName`, and `ThreadId`
- All four `BackgroundService` consumers (`InvoiceCreatedConsumer`, `QuoteCreatedConsumer`, `QuoteApprovalRequestedConsumer`, `InvoiceGeneratedConsumer`) manually extract W3C `traceparent`/`tracestate` headers from incoming Kafka messages and start a child `Activity` restoring the trace context from the producer
- **`Serilog.Enrichers.Span`** embeds the restored `TraceId` into every log entry inside the consumer — consumer logs in `PdfGeneratorService` and `EmailNotificationService` share the same `TraceId` as the originating HTTP request in `InvoiceTrackerApi`
- Kafka consumer events logged with topic, partition, and offset for traceability
- Email delivery and PDF generation outcomes logged for operational visibility

### Configuration

- All external dependencies (Kafka bootstrap servers, database connection strings, SMTP settings, MinIO credentials) configured via `appsettings.json`
- Environment-specific overrides via `appsettings.Development.json`
- No hardcoded connection strings — production deployment only requires configuration changes

### Resilience Considerations

- Kafka consumers use consumer groups with automatic offset commits — if a service restarts, it resumes from the last committed offset
- Events carry minimal payloads (entity ID + timestamp) — full data fetched from the database at processing time, ensuring freshness
- If a consumer fails to process an event, Kafka retains it for reprocessing based on retention policy
