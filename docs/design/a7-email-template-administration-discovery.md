# A7 Discovery — Email Template Administration

**Status:** Discovery / Inspection (no implementation)
**Date:** 2026-07-22
**Milestone:** 8 (Phase A7)
**Author:** ATLAS Developer agent (per mandatory inspection before implementation)

## 0. Method & Scope Guardrails

Inspection only. Per the task constraints, this document does **not** implement
anything, does **not** redesign `IEmailTemplateRenderer`, and does **not** replace
the email sending infrastructure. It inventories what exists, identifies gaps, and
recommends a minimal implementation scope that reuses existing infrastructure.

---

## 1. Architecture Inventory

### 1.1 Renderer contract — EXISTS

- **`IEmailTemplateRenderer`** (`src/ATLAS.Application/Interfaces/IEmailTemplateRenderer.cs`)
  - Single method: `Task<string> RenderAsync(string templateName, object model, CancellationToken)`.
  - Returns the rendered **plain-text** body.

### 1.2 Renderer implementation — EXISTS

- **`EmailTemplateRenderer`** (`src/ATLAS.Infrastructure/Services/EmailTemplateRenderer.cs`)
  - Reads a `.txt` file from disk: `Path.Combine(_templatePath, $"{templateName}.txt")`.
  - `_templatePath` = `configuration["Email:Templates:Path"]` **or** fallback
    `AppContext.BaseDirectory/Templates/Emails`.
  - **Placeholder replacement:** reflection over `model.GetType().GetProperties()`;
    replaces `{{PropertyName}}` with `prop.GetValue(model)?.ToString() ?? ""`.
  - Plain text only (`isHtml: false` at all call sites). No HTML, no conditional
    logic, no loops, no localization tokens.

### 1.3 Template storage — FILE SYSTEM (no DB)

- Location: `src/ATLAS.Infrastructure/Templates/Emails/*.txt` (source tree).
- Copied to output by `ATLAS.Infrastructure.csproj`:
  `<Content Include="Templates\Emails\*.txt"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`.
- **4 templates exist:**

  | File | Placeholders used |
  | ------ | ------------------- |
  | `ApprovalNotification.txt` | `ApplicationNumber`, `PermitTypeName` |
  | `InfoRequestNotification.txt` | `ApplicationNumber`, `PermitTypeName`, `Message` |
  | `RejectionNotification.txt` | `ApplicationNumber`, `PermitTypeName`, `ReasonCode` |
  | `SubmissionConfirmation.txt` | `ApplicationNumber`, `PermitTypeName`, `Status` |

### 1.4 Persistence — NONE (for templates)

- No `EmailTemplate` entity, no `IEmailTemplateRepository`, no table, no migration.
- Templates are **static deployment artifacts**, not runtime data.

### 1.5 Rendering pipeline — EXISTS (event-driven)

- Four MediatR `INotificationHandler`s in `src/ATLAS.Infrastructure/EventHandlers/`:
  - `ApplicationSubmittedEmailHandler` → `SubmissionConfirmation`
  - `ApplicationApprovedEmailHandler` → `ApprovalNotification`
  - `ApplicationRejectedEmailHandler` → `RejectionNotification`
  - `ApplicationInfoRequestedEmailHandler` → `InfoRequestNotification`
- Flow: domain event → handler resolves `ApplicationSummaryDto` (via
  `IApplicationRepository`, `IUserRepository`, `IPermitTypeRepository`) →
  `IEmailTemplateRenderer.RenderAsync(templateName, model)` →
  `IEmailService.SendAsync(...)`.

### 1.6 Template model — EXISTS

- **`ApplicationSummaryDto`** (`src/ATLAS.Application/DTOs`) is the only model used
  by all four handlers. Its properties define the available `{{...}}` placeholders.

### 1.7 Preview support — NONE

- No preview endpoint, no preview query, no UI preview. The renderer is only
  invoked from event handlers at send time.

### 1.8 Localization — NONE

- No `.resx`, no `CultureInfo` handling, no language selection. Templates are
  English-only and hardcoded.

### 1.9 Email sending pipeline — EXISTS (dev SMTP only)

- **`IEmailService`** (`src/ATLAS.Application/Interfaces/IEmailService.cs`):
  `Task SendAsync(string to, string subject, string body, bool isHtml, CancellationToken)`.
- **`SmtpEmailService`** (`src/ATLAS.Infrastructure/Services/SmtpEmailService.cs`):
  uses `System.Net.Mail.SmtpClient` with config `Email:Smtp`
  (`Host`,`Port`,`EnableSsl`,`Username`,`Password`,`From`,`FromName`).
  - Send failures are **swallowed** (logged, not rethrown) so email never blocks workflow.

### 1.10 Azure Communication Services — NOT PRESENT

- No `Azure.Communication.*` package, no `EmailClient`, no ACS integration anywhere
  in the solution. Sending is dev SMTP only.

### 1.11 Repositories — NONE for templates

- Existing repos: `IApplicationRepository`, `IUserRepository`, `IPermitTypeRepository`,
  `IAuditLogRepository`. No email-template repository.

### 1.12 CQRS — NONE for templates

- No email-template commands or queries.
- `GetAdminDashboardQuery` / `AdminDashboardDto` reference email only via a
  **hardcoded placeholder**: `ActiveEmailTemplateCount = 0` (with a doc comment
  "Placeholder until Email Template management is implemented").

### 1.13 API endpoints — NONE for templates

- OpenAPI (`openapi/atlas-api.yaml`) has no email/template paths.
- No generated or hand-written controller exposes templates.

### 1.14 Existing Blazor pages — PLACEHOLDER ONLY

- **`EmailTemplates.razor`** (`src/ATLAS.Blazor/Components/Pages/Admin/`):
  - Route `/admin/email-templates`, `[Authorize(Roles = "Admin")]`, InteractiveServer.
  - Uses `PageHeader` + `EmptyState` ("Email Templates coming soon").
  - **No code-behind**, no data binding, no editor.
- **`AdminDashboard.razor`** displays `_viewModel.Summary!.ActiveEmailTemplateCount`
  (currently always `0`).

### 1.15 Reusable UI building blocks — EXIST

- `PageHeader`, `EmptyState` (shared admin components).
- `PermitTypeSettings.razor.cs` demonstrates the established editor pattern:
  load via `Mediator.Send(query)` → `IsLoading`/`HasError`/`NotFound` states →
  edit `ViewModel` → `Mediator.Send(command)` → `IsSaving`/`SaveMessage`/`ErrorMessage`
  → reload. This is the pattern A7 should mirror.

---

## 2. Gap Analysis

| Capability | Status | Gap for A7 |
| ------------ | -------- | ----------- |
| Render templates | ✅ Exists | Reuse as-is (no change) |
| Store templates | ⚠️ File system only | No runtime-editable store; files are deployment artifacts |
| Persist edits | ❌ Missing | No write path; admin cannot change content without redeploy |
| List templates | ❌ Missing | No query enumerates available templates |
| Read single template | ❌ Missing | No query returns content for editing |
| Preview render | ❌ Missing | Renderer callable but no preview query/UI |
| API surface | ❌ Missing | No controller/contracts for templates |
| CQRS | ❌ Missing | No queries/commands |
| Localization | ❌ Missing (out of scope per PRD) | Confirm out of scope |
| ACS / prod sending | ❌ Missing (out of scope) | Do not add; keep SMTP |

**Core gap:** templates are **files**, not data. To let an administrator edit them
through the portal, A7 must introduce *some* read/write mechanism. The least-invasive
option keeps the renderer untouched (it reads files) and adds a **writable templates
directory** addressed by `Email:Templates:Path`, with CQRS + API + UI layered on top.
A DB-backed store would require changing the renderer's source (file → DB), which
conflicts with the "do not redesign the renderer" constraint.

---

## 3. Recommended Implementation Scope

**Principle:** Reuse the renderer, the SMTP sender, the admin UI patterns, and the
MediatR/CQRS style. Introduce only what is necessary to make the 4 existing file-based
templates editable + previewable from the Administration Portal.

### 3.1 Application layer (CQRS)

- `GetEmailTemplatesQuery` → returns list of `{ Name, Path/RelativeKey }` (enumeration
  of the 4 known templates; no DB).
- `GetEmailTemplateByNameQuery` → returns `{ Name, Content }` for editing.
- `UpdateEmailTemplateCommand` → writes `Content` back to the writable templates
  directory (validated to stay within that directory — path-traversal guard).
- `PreviewEmailTemplateQuery` → invokes the **existing** `IEmailTemplateRenderer`
  with a sample/fixed `ApplicationSummaryDto` (or admin-supplied test values) and
  returns the rendered string. **No renderer change.**

### 3.2 Infrastructure layer

- A small `IEmailTemplateStore` (file-based) that reads/writes `.txt` under the
  configurable `Email:Templates:Path`. Keeps `EmailTemplateRenderer` unchanged.
- Path-traversal / extension (`.txt` only) validation in the store + command.

### 3.3 API layer

- Generated/hand-written controller exposing the 3 queries + 1 command, mapped to the
  `Admin` policy (consistent with `auditlogs` → `Admin` convention). Add to OpenAPI
  and regenerate NSwag contracts.

### 3.4 Blazor (Administration Portal)

- Replace `EmailTemplates.razor` placeholder with:
  - **List view:** template names (reuse `PageHeader`, list/table pattern).
  - **Editor:** textarea bound to `Content`, `IsLoading`/`IsSaving`/`SaveMessage`/
    `ErrorMessage` states (mirror `PermitTypeSettings.razor.cs`).
  - **Save:** `Mediator.Send(UpdateEmailTemplateCommand)`.
  - **Preview:** `Mediator.Send(PreviewEmailTemplateQuery)` → render result panel.
- Wire `ActiveEmailTemplateCount` to the real enumerated count (replace hardcoded `0`).

### 3.5 Explicitly OUT of scope (per constraints + PRD)

- No renderer redesign; no HTML/Markdown rendering; no conditional/loop syntax.
- No ACS / production email provider; SMTP stays.
- No localization / multi-language templates.
- No template versioning/history/rollback (PRD §14 post-MVP).
- No new template types beyond the 4 that exist.

---

## 4. Risks

1. **File-based persistence is deployment-fragile.** Writing into the app bundle
   (`AppContext.BaseDirectory`) does not survive container redeploys and is not shared
   across instances. Mitigation: `Email:Templates:Path` must point to a writable,
   persistent volume/mount in all environments.
2. **Renderer path coupling.** If `Email:Templates:Path` is not consistently set,
   preview (DB/store) and render (file) could diverge. Mitigation: single store
   abstraction used by both read-for-edit and the renderer path.
3. **Broken placeholders.** An admin can edit `{{...}}` tokens and break rendering.
   Mitigation: editor surfaces the known placeholders (derived from
   `ApplicationSummaryDto`) and optionally validates token names on save.
4. **Path traversal / arbitrary write.** Writing file content must be confined to the
   templates directory with `.txt`-only enforcement and name allow-listing.
5. **No send verification.** SMTP failures are swallowed; preview only validates
   rendering, not delivery. Acceptable for A7 (documented).
6. **Dashboard count drift.** `ActiveEmailTemplateCount` hardcoded `0` must be wired
   to the real enumeration or the dashboard will mislead admins.

---

## 5. Questions Requiring Clarification

1. **Persistence target:** file system (writable volume via `Email:Templates:Path`)
   vs a new DB table? A DB store would require the renderer to read from DB
   (renderer change) — is that acceptable, or must the renderer stay file-based?
2. **Environment strategy:** are templates shared across environments or
   environment-specific (dev vs prod)?
3. **Preview model:** use a fixed sample `ApplicationSummaryDto` (dummy data) or let
   the admin supply test placeholder values?
4. **Placeholder validation:** should the editor validate `{{...}}` tokens against
   known `ApplicationSummaryDto` properties, or allow free-form editing?
5. **Dashboard wiring:** should `ActiveEmailTemplateCount` be wired to the real
   enumerated count as part of A7?
6. **Localization:** confirm English-only templates remain out of scope for A7?
7. **Versioning:** confirm no history/rollback is required (PRD lists it post-MVP)?
8. **Authorization:** confirm `Admin`-only access (consistent with the existing
   `EmailTemplates.razor` `[Authorize(Roles="Admin")]`) is sufficient?

---

## 6. Conclusion

All *rendering* and *sending* infrastructure already exists and must be reused
unchanged. The only genuine gap for A7 is the **absence of any editable, enumerable
store for the 4 existing file-based templates**, plus the missing CQRS/API/UI surface
to reach them from the Administration Portal. Recommended scope is a thin,
file-backed (or volume-backed) store with list/read/update/preview CQRS, a small
admin API, and a `PermitTypeSettings`-style Blazor editor — with no renderer or
email-infrastructure changes.
