# Quote & Invoice Data Specification — Roadmap

> **Date:** 2026-02-25  
> **Status:** Draft  
> **Scope:** Define the exact data model for quotes and invoices, update entities/DTOs/templates/frontend to match, and produce correct PDF documents.

---

## 1. Document Data Groups (Specification)

Both **Quotation** and **Invoice** documents share a common structure composed of five groups. Templates use placeholder tokens; if a value is absent from the payload the corresponding section is omitted from the generated PDF.

### Group 1 — Title

| Field | Quote value | Invoice value |
|-------|-------------|---------------|
| `DocumentTitle` | `"QUOTATION"` | `"INVOICE"` |

### Group 2 — Client Details

| Placeholder | Source | Required |
|-------------|--------|----------|
| `{{ClientName}}` | `Client.Name` | ✅ |
| `{{ClientTelephone}}` | `Client.Cellphone` | ❌ |
| `{{ClientEmail}}` | `Client.Email` | ❌ |
| `{{ClientAddress}}` | `Client.Address` | ❌ |
| `{{ClientVatNumber}}` | `Client.VatNumber` | ❌ |

### Group 3 — Issuing Organisation & Document Meta

| Placeholder | Source | Required |
|-------------|--------|----------|
| `{{OrganizationName}}` | `Organization.Name` | ✅ |
| `{{OrganizationAddress}}` | `Organization.Address` (formatted) | ❌ |
| `{{OrganizationPhone}}` | `Organization.Phone` | ❌ |
| `{{OrganizationEmail}}` | `Organization.Email` | ❌ |
| `{{DocumentNumber}}` | Quote: `Q-{Id}` / Invoice: `INV-{Id}` | ✅ |
| `{{DateCreated}}` | `Quote.DateCreated` / `Invoice.DateCreated` | ✅ |

### Group 4 — Line Items & VAT Handling

Each item row:

| Placeholder | Source |
|-------------|--------|
| `{{ItemDescription}}` | `QuoteItem.Description` / `InvoiceItem.Description` |
| `{{ItemQuantity}}` | `QuoteItem.Quantity` / `InvoiceItem.Quantity` |
| `{{ItemUnitPrice}}` | `QuoteItem.PricePerUnit` / `InvoiceItem.PricePerUnit` |
| `{{ItemTotal}}` | `Quantity × PricePerUnit` |

**VAT toggle** (`VatInclusive: bool`):

| Scenario | What the document shows |
|----------|------------------------|
| **VAT Inclusive** (`true`) | Item prices include VAT. Show only the **Total** (no VAT breakdown). |
| **VAT Exclusive** (`false`) | Item prices exclude VAT. Show **Subtotal (excl. VAT)**, **VAT Amount (15%)**, and **Total (incl. VAT)**. |

Totals placeholders:

| Placeholder | Shown when |
|-------------|------------|
| `{{Subtotal}}` | Always |
| `{{VatAmount}}` | Only when `VatInclusive == false` |
| `{{Total}}` | Always |
| `{{VatNote}}` | `"All prices include VAT"` when inclusive; `"VAT calculated at 15%"` when exclusive |

### Group 5 — Footer (differs per document type)

#### Quote Footer

> *"To accept this quotation, please respond to the email you received. You will then receive an invoice for payment. If you wish to make amendments to this quotation, please contact us at {{OrganizationPhone}}."*

#### Invoice Footer

| Placeholder | Source |
|-------------|--------|
| `{{BankName}}` | `BankAccount.BankName` |
| `{{BranchCode}}` | `BankAccount.BranchCode` |
| `{{AccountNumber}}` | `BankAccount.AccountNumber` |
| `{{AccountType}}` | `BankAccount.AccountType` |
| `{{PayByDate}}` | `Invoice.PayByDate` |
| `{{ProofOfPaymentEmail}}` | `Organization.Email` |

---

## 2. Current State Analysis

### What exists today

| Layer | Current state | Gap |
|-------|--------------|-----|
| **`Quote` entity** | Has `ClientId`, `Items`, `TemplateId`, `OrganizationId` | Missing `VatInclusive` flag |
| **`Invoice` entity** | Has `ClientId`, `Items`, `TemplateId`, `OrganizationId`, `PayByDate` | Missing `VatInclusive` flag |
| **`Client` entity** | Has `Name`, `Email`, `Cellphone`, `Address`, `VatNumber` | ✅ Complete for Group 2 |
| **`Organization` entity** | Has `Name`, `Address`, `Phone`, `Email`, `BankAccountIds` | ✅ Complete for Group 3 & 5 |
| **`BankAccount` entity** | Has `BankName`, `BranchCode`, `AccountNumber`, `AccountType` | ✅ Complete for Group 5 |
| **`CreateQuoteRequest` DTO** | `ClientId`, `TemplateId`, `Items` | Missing `VatInclusive` |
| **`CreateInvoiceRequest` DTO** | `ClientId`, `TemplateId`, `Items`, `PayByDays` | Missing `VatInclusive` |
| **`ConvertQuoteToInvoiceRequest` DTO** | `QuoteId`, `TemplateId`, `PayByDays` | Missing `VatInclusive` |
| **`PdfGenerationService`** | `PopulateTemplate()` / `PopulateQuoteTemplate()` — uses basic placeholders, no org data, no VAT logic, no bank details | Needs full rewrite of placeholder population |
| **Kafka consumers** | Fetch `Invoice`/`Quote` with `Items` + `Client` | Must also `.Include(Organization)` with `Address` and fetch `BankAccounts` |
| **HTML templates** | Hardcoded company info, no VAT section, no bank details, inconsistent placeholder syntax (`{x}` vs `{{x}}`) | Need complete replacement |
| **Frontend modals** | `NewQuoteModal`, `NewInvoiceModal`, `ConvertToInvoiceModal` | Missing VAT inclusive/exclusive toggle |

---

## 3. Roadmap — Step-by-Step

### Phase 1: Database & Entity Changes

#### 1.1 Add `VatInclusive` to `Quote` entity
- **File:** [`Quote.cs`](shared/Shared.Database/Models/Quote.cs)
- Add `public bool VatInclusive { get; set; } = true;`

#### 1.2 Add `VatInclusive` to `Invoice` entity
- **File:** [`Invoice.cs`](shared/Shared.Database/Models/Invoice.cs)
- Add `public bool VatInclusive { get; set; } = true;`

#### 1.3 Create EF Core migration
- Run `dotnet ef migrations add AddVatInclusiveFlag` to generate the migration.
- Apply with `dotnet ef database update`.

---

### Phase 2: DTO & API Changes

#### 2.1 Update `CreateQuoteRequest`
- **File:** [`CreateQuoteRequest.cs`](apps/client/backend/InvoiceTrackerApi/DTOs/Quote/Requests/CreateQuoteRequest.cs)
- Add `public bool VatInclusive { get; set; } = true;`

#### 2.2 Update `CreateInvoiceRequest`
- **File:** [`CreateInvoiceRequest.cs`](apps/client/backend/InvoiceTrackerApi/DTOs/Invoice/Requests/CreateInvoiceRequest.cs)
- Add `public bool VatInclusive { get; set; } = true;`

#### 2.3 Update `ConvertQuoteToInvoiceRequest`
- **File:** [`ConvertQuoteToInvoiceRequest.cs`](apps/client/backend/InvoiceTrackerApi/DTOs/Invoice/Requests/ConvertQuoteToInvoiceRequest.cs)
- Add `public bool? VatInclusive { get; set; }` (nullable — inherits from quote if not specified)

#### 2.4 Update Quote & Invoice Response DTOs
- Ensure the response DTOs expose `VatInclusive` so the frontend can display the current state.

#### 2.5 Update service layer
- **File:** [`InvoiceService.cs`](apps/client/backend/InvoiceTrackerApi/Services/Invoice/InvoiceService.cs)
  - In `CreateInvoiceAsync()`: map `request.VatInclusive` → `invoice.VatInclusive`
  - In `ConvertQuoteToInvoiceAsync()`: use `request.VatInclusive ?? quote.VatInclusive`
- **Quote service** (equivalent file): map `request.VatInclusive` → `quote.VatInclusive`

#### 2.6 Regenerate OpenAPI client
- After API changes, regenerate the TypeScript API client so the frontend picks up the new fields.

---

### Phase 3: PDF Generation Service — Template Population Rewrite

#### 3.1 Standardise placeholder syntax
- Adopt `{{PlaceholderName}}` consistently across both templates (the quote template already uses this; the invoice template currently uses `{PlaceholderName}`).

#### 3.2 Update Kafka consumers to fetch full data graph
- **Files:**
  - [`InvoiceCreatedConsumer.cs`](apps/client/backend/PdfGeneratorService/BackgroundServices/InvoiceCreatedConsumer.cs)
  - [`QuoteCreatedConsumer.cs`](apps/client/backend/PdfGeneratorService/BackgroundServices/QuoteCreatedConsumer.cs)
- Change the EF query to include:
  ```csharp
  .Include(i => i.Client)
  .Include(i => i.Organization)
      .ThenInclude(o => o.Address)
  ```
- For invoices, also fetch the organisation's active bank accounts:
  ```csharp
  var bankAccounts = await dbContext.BankAccounts
      .Where(b => b.OrganizationId == invoice.OrganizationId && b.Active)
      .ToListAsync();
  ```

#### 3.3 Rewrite `PopulateTemplate()` in `PdfGenerationService`
- **File:** [`PdfGenerationService.cs`](apps/client/backend/PdfGeneratorService/Services/Generation/PdfGenerationService.cs)
- Replace all placeholders from Groups 1–5.
- Implement VAT logic:
  ```
  if VatInclusive:
      hide {{VatSection}}
      {{VatNote}} = "All prices include VAT"
  else:
      subtotal = sum of item totals
      vatAmount = subtotal * 0.15
      total = subtotal + vatAmount
      show {{VatSection}} with {{Subtotal}}, {{VatAmount}}, {{Total}}
      {{VatNote}} = "VAT calculated at 15%"
  ```
- Conditionally render sections: if a placeholder value is `null` or empty, remove the entire HTML block containing it (e.g., if `ClientVatNumber` is null, omit the VAT number line).

#### 3.4 Pass `Organization` and `BankAccount` data to the generation methods
- Update `GeneratePdfFromInvoiceAsync()` and `GeneratePdfFromQuoteAsync()` signatures (or pass the full entity graph including navigation properties).

---

### Phase 4: New HTML Templates

#### 4.1 Create new `InvoiceTemplate.html`
- **File:** [`InvoiceTemplate.html`](apps/client/backend/PdfGeneratorService/Templates/InvoiceTemplate.html)
- Must include all Group 1–5 placeholders.
- The VAT section should be wrapped in a conditional block (e.g., `<!-- VAT_SECTION_START -->...<!-- VAT_SECTION_END -->`) that the C# code can strip when `VatInclusive == true`.
- Footer must include bank details section and pay-by-date.

#### 4.2 Create new `QuoteTemplate.html`
- **File:** [`QuoteTemplate.html`](apps/client/backend/PdfGeneratorService/Templates/QuoteTemplate.html)
- Must include all Group 1–5 placeholders.
- Same VAT conditional block approach.
- Footer must include the acceptance/rejection note with `{{OrganizationPhone}}`.

#### 4.3 Update `MinioInitializationService`
- **File:** [`MinioInitializationService.cs`](apps/client/backend/PdfGeneratorService/BackgroundServices/MinioInitializationService.cs)
- The service already uploads default templates on startup if they don't exist. Since the templates in MinIO are now outdated, either:
  - **Option A:** Delete the old templates from MinIO and let the service re-upload the new ones on next startup.
  - **Option B:** Add a version check / force-overwrite flag so updated templates replace old ones.
- Recommended: **Option B** — add a version constant and always overwrite if the local template is newer.

---

### Phase 5: Frontend Changes

#### 5.1 Add VAT toggle to `NewQuoteModal`
- **File:** [`NewQuoteModal.vue`](apps/client/frontend/src/components/modals/NewQuoteModal.vue)
- Add a toggle/switch: **"Prices include VAT"** (default: `true`).
- Include `vatInclusive` in the emitted `save` payload.
- When `vatInclusive` is `false`, show a computed VAT breakdown below the total in the modal preview.

#### 5.2 Add VAT toggle to `NewInvoiceModal`
- **File:** [`NewInvoiceModal.vue`](apps/client/frontend/src/components/modals/NewInvoiceModal.vue)
- Same toggle as above.
- Include `vatInclusive` in the emitted `save` payload.

#### 5.3 Update `ConvertToInvoiceModal`
- **File:** [`ConvertToInvoiceModal.vue`](apps/client/frontend/src/components/modals/ConvertToInvoiceModal.vue)
- Add optional VAT toggle (pre-filled from the source quote's `vatInclusive` value).
- Include `vatInclusive` in the emitted `confirm` payload.

#### 5.4 Update parent views (`Quotes.vue`, `Invoices.vue`)
- Pass the new `vatInclusive` field through to the API calls in [`api.ts`](apps/client/frontend/src/services/api.ts).

#### 5.5 Update API service layer
- **File:** [`api.ts`](apps/client/frontend/src/services/api.ts)
- Ensure `createQuote`, `createInvoice`, and `convertQuoteToInvoice` pass `vatInclusive` in the request body.

---

### Phase 6: Testing & Verification

#### 6.1 Unit tests
- Test VAT calculation logic (inclusive vs exclusive) in `PdfGenerationService`.
- Test placeholder replacement with missing optional fields (ensure graceful omission).

#### 6.2 Integration tests
- Create a quote with `VatInclusive = true` → verify PDF has no VAT breakdown.
- Create a quote with `VatInclusive = false` → verify PDF shows subtotal, VAT, and total.
- Create an invoice with bank details → verify PDF footer shows bank info.
- Create a quote → verify PDF footer shows acceptance/rejection note.
- Test with a client that has no `VatNumber` or `Address` → verify those lines are omitted.

#### 6.3 End-to-end smoke test
- Full flow: Create quote → approve → convert to invoice → verify both PDFs are correct.

---

## 4. Template Placeholder Reference (Complete)

### Shared placeholders (both templates)

```
{{DocumentTitle}}         — "QUOTATION" or "INVOICE"
{{DocumentNumber}}        — "Q-123" or "INV-456"
{{DateCreated}}           — "February 25, 2026"

{{ClientName}}            — Client name
{{ClientTelephone}}       — Client phone (omit if empty)
{{ClientEmail}}           — Client email (omit if empty)
{{ClientAddress}}         — Client address (omit if empty)
{{ClientVatNumber}}       — Client VAT number (omit if empty)

{{OrganizationName}}      — Issuing org name
{{OrganizationAddress}}   — Org street, city, state, postal code, country
{{OrganizationPhone}}     — Org phone (omit if empty)
{{OrganizationEmail}}     — Org email (omit if empty)

{{Items}}                 — Rendered item rows
{{Subtotal}}              — Sum of all item totals
{{VatAmount}}             — 15% of subtotal (only when VatInclusive == false)
{{Total}}                 — Final total
{{VatNote}}               — Contextual note about VAT
```

### Invoice-only placeholders

```
{{PayByDate}}             — "March 27, 2026"
{{BankName}}              — e.g. "FNB"
{{BranchCode}}            — e.g. "250655"
{{AccountNumber}}         — e.g. "62012345678"
{{AccountType}}           — e.g. "Cheque"
{{ProofOfPaymentEmail}}   — Organization email for PoP
```

### Quote-only placeholders

```
{{OrganizationPhone}}     — (reused in footer acceptance note)
```

---

## 5. File Change Summary

| File | Action |
|------|--------|
| `shared/Shared.Database/Models/Quote.cs` | Add `VatInclusive` property |
| `shared/Shared.Database/Models/Invoice.cs` | Add `VatInclusive` property |
| `shared/Shared.Database/Migrations/` | New migration for `VatInclusive` column |
| `apps/client/backend/InvoiceTrackerApi/DTOs/Quote/Requests/CreateQuoteRequest.cs` | Add `VatInclusive` |
| `apps/client/backend/InvoiceTrackerApi/DTOs/Invoice/Requests/CreateInvoiceRequest.cs` | Add `VatInclusive` |
| `apps/client/backend/InvoiceTrackerApi/DTOs/Invoice/Requests/ConvertQuoteToInvoiceRequest.cs` | Add `VatInclusive` |
| Quote & Invoice Response DTOs | Add `VatInclusive` |
| `apps/client/backend/InvoiceTrackerApi/Services/Invoice/InvoiceService.cs` | Map `VatInclusive` |
| Quote service (equivalent) | Map `VatInclusive` |
| `apps/client/backend/PdfGeneratorService/Services/Generation/PdfGenerationService.cs` | Full rewrite of `PopulateTemplate()` and `PopulateQuoteTemplate()` |
| `apps/client/backend/PdfGeneratorService/BackgroundServices/InvoiceCreatedConsumer.cs` | Include `Organization`, `Address`, fetch `BankAccounts` |
| `apps/client/backend/PdfGeneratorService/BackgroundServices/QuoteCreatedConsumer.cs` | Include `Organization`, `Address` |
| `apps/client/backend/PdfGeneratorService/BackgroundServices/MinioInitializationService.cs` | Force-overwrite outdated templates |
| `apps/client/backend/PdfGeneratorService/Templates/InvoiceTemplate.html` | Complete replacement |
| `apps/client/backend/PdfGeneratorService/Templates/QuoteTemplate.html` | Complete replacement |
| `apps/client/frontend/src/components/modals/NewQuoteModal.vue` | Add VAT toggle |
| `apps/client/frontend/src/components/modals/NewInvoiceModal.vue` | Add VAT toggle |
| `apps/client/frontend/src/components/modals/ConvertToInvoiceModal.vue` | Add VAT toggle |
| `apps/client/frontend/src/services/api.ts` | Pass `vatInclusive` in requests |
| `apps/client/frontend/src/views/Quotes.vue` | Wire VAT toggle to API call |
| `apps/client/frontend/src/views/Invoices.vue` | Wire VAT toggle to API call |
| OpenAPI / generated client | Regenerate after API changes |

---

## 6. Execution Order

```
Phase 1  →  Phase 2  →  Phase 3  →  Phase 4  →  Phase 5  →  Phase 6
 (DB)        (API)       (PDF svc)   (HTML)      (Frontend)   (Test)
```

Each phase can be implemented and verified independently before moving to the next. Phase 3 and Phase 4 are tightly coupled and should be done together. Phase 5 depends on Phase 2 (API changes) being deployed first.
