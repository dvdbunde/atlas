---
title: "Milestone 6 — Architecture Review"
date: "2026-06-29"
status: "Complete"
reviewer: "Senior Architecture Reviewer"
tags: ["architecture-review", "milestone-6", "document-management", "clean-architecture", "ddd", "cqrs"]
---

# Milestone 6 — Full Architecture Review

## Context

Milestone 6 (Document Management) has been fully implemented. Implementation completed all planned phases, including:

- Azure Blob Storage integration
- Document metadata persistence
- `UploadDocumentCommand` / `DeleteDocumentCommand` / download workflow
- Requirement-centric document UI
- Draft creation & edit workflow
- Submission-time document validation
- End-to-end citizen document workflow
- Transaction pipeline refactoring (AR-001)
- Expanded automated test coverage
- Documentation updates

This review is a holistic architecture assessment of the completed milestone. It is **not** a pull request review — previous code reviews were performed incrementally during implementation.

## Objective

Determine whether Milestone 6 is architecturally complete and ready to become the permanent foundation for future milestones.

---

## 1. Domain Layer

| Aspect | Rating |
|--------|--------|
| Aggregate consistency | Good |
| Business rules inside domain | Excellent |
| Infrastructure leakage | Excellent |
| Aggregate invariants enforcement | Needs Improvement |

### Findings

The domain model remains clean and well-structured. The `Application` aggregate root properly owns `Document`, `Review`, and `ApplicationFieldValue` through private backing fields with `IReadOnlyList` exposure — no external code can bypass aggregate methods.

**Document entity** (`src/ATLAS.Domain/Entities/Document.cs`) uses an `internal` constructor, which enforces the aggregate boundary — only `Application.AddDocument()` can create `Document` instances. Constructor validation is thorough (empty IDs, file size limits, name length constraints).

**Domain events** `DocumentUploadedEvent` and `DocumentDownloadedEvent` are properly defined as MediatR `INotification`. The legacy `DocumentType` enum is correctly deprecated with `[Obsolete]` — file type validation is now handled through MIME types and `DocumentRequirement` value objects.

**ApplicationAggregate.ValidateInvariants()** is defined but **never called** by any command handler. This is a gap — the method checks invariants like rejection-requires-reason-code and document-aggregate-ownership, but no handler invokes it. If a future handler modifies the aggregate incorrectly, this safety net provides no protection.

> **Critical Finding C-01**: `ValidateInvariants()` exists as a design artifact but is dead code. It should be called in every command handler that modifies the aggregate.

---

## 2. Application Layer

| Aspect | Rating |
|--------|--------|
| CQRS separation | Excellent |
| Business logic duplication | Good |
| Command handler design | Excellent |
| DTO design | Good |

### Findings

CQRS separation is enforced through the `ICommand<T>` marker interface. Commands implement `ICommand<T>`; queries implement plain `IRequest<T>`. The `TransactionBehavior` constrains itself to `ICommand<T>` only, so queries never accidentally trigger a commit — **excellent design**.

**UploadDocumentCommandHandler** (`src/ATLAS.Application/Commands/Documents/UploadDocumentCommand.cs`) is well-structured with clear steps:

1. Ownership verification (CitizenId check)
2. PermitType DocumentRequirement validation (extension + size)
3. Virus scan invocation
4. Blob upload via `IFileStorageService`
5. Document entity creation via `application.AddDocument()`
6. Repository update
7. Domain event publication

**DeleteDocumentCommandHandler** handles blob-not-found gracefully (logs warning, proceeds with metadata removal).

**SubmitDraftCommandHandler** validates:

- Required field values are present
- No extraneous fields exist
- Required document uploads exist
- All within the command handler — some of this validation arguably belongs in the domain layer

> **Critical Finding C-02**: The `SubmitApplicationCommand` is marked `[Obsolete]` (comment says "Use `CreateDraftCommand` + `SubmitDraftCommand` instead") but is still wired in `ApplicationsController.ApplicationsPost`. This old monolithic submit bypasses the draft workflow entirely, creating two inconsistent code paths for application creation.

---

## 3. MediatR Pipeline

| Aspect | Rating |
|--------|--------|
| Pipeline ordering | Excellent |
| Transaction boundaries | Good |
| SaveChanges frequency | Excellent |
| AR-001 implementation | Good |

### Findings

The pipeline is configured as:

```
ValidationBehavior
  ↓
UserSynchronizationBehavior
  ↓
TransactionBehavior
  ↓
Command Handler
```

**ValidationBehavior** — runs first for all request types. FluentValidation validators from the Application assembly are auto-discovered.

**UserSynchronizationBehavior** — synchronizes Entra ID claims with Domain User before every authenticated request. Thin behavior delegating to `IIdentityResolver`.

**TransactionBehavior** — constrained to `ICommand<TResponse>` only (queries pass through without committing). Calls `SaveChangesAsync()` exactly once after `next()` succeeds. If the handler throws, no commit occurs.

**AR-001 (Transaction Isolation) Design:** The `UserSynchronizationBehavior` commits its changes via `IdentityResolver` in a **separate transaction** from the handler's `TransactionBehavior`. This means if the handler fails after user sync succeeds, the user sync changes are already committed. The code acknowledges this trade-off in XML comments.

> **Major Finding M-03**: No `IDbContextTransaction` wrapping exists. The two-phase commit (sync then handler) means partial commits are possible. A future improvement should either:
> - (a) Move transaction orchestration to a single `IDbContextTransaction`, or
> - (b) Accept the trade-off (as AR-001 does) and document the window explicitly.

---

## 4. Infrastructure Layer

| Aspect | Rating |
|--------|--------|
| Blob storage abstraction | Excellent |
| BlobStorageService implementation | Excellent |
| EF Core mapping | Excellent |
| Owned entity configuration | Excellent |
| Repository design | Good |
| UnitOfWork design | Good |

### Findings

**IFileStorageService** interface lives in the Application layer — Clean Architecture compliance. The interface is clean (`UploadAsync`, `DownloadAsync`, `GenerateDownloadSasUriAsync`, `DeleteAsync`).

**BlobStorageService** is well-implemented:

- SAS token generation with proper `StartsOn` (5-minute clock skew buffer) and configured expiry
- `DeleteAsync` checks existence first, returns bool
- Blob path parsing handles leading slash trimming correctly
- Container auto-creation via `CreateIfNotExistsAsync`

**EF Core Configuration** (`ApplicationConfiguration.cs`):

- `OwnsMany` for Documents, Reviews, and FieldValues — correct aggregate ownership
- `ValueGeneratedNever()` on owned entity IDs — prevents EF from generating IDs
- Proper max lengths and nullability

> **Major Finding M-01**: `DocumentDto.BlobUrl` is exposed through the API response (`GetApplicationByIdQuery` populates it). ADR-015 explicitly states "BlobUrl is never exposed directly." The download endpoint correctly returns a SAS URI, but the detail endpoint leaks the raw BlobUrl.

> **Major Finding M-02**: Event handlers (`DocumentUploadedEventHandler`, `DocumentDownloadedEventHandler`) write `AuditLog` entries directly via `IAuditLogRepository`, which commits **outside** the `TransactionBehavior` scope. If the transaction rolls back, audit log entries remain committed — and vice versa.

> **Major Finding M-04**: `AuditLogRepository` exposes `UpdateAsync` and `DeleteAsync` for what should be an immutable audit trail. These methods are never called (no handler invokes them), but they represent a design inconsistency.

> **Minor Finding**: `ApplicationRepository.GetByIdAsync()` does not use `.Include()` for owned entities. EF Core's default behavior may or may not load `Documents`/`Reviews`/`FieldValues` depending on lazy loading configuration. The current code works because owned entities are loaded by default in many EF Core configurations, but explicit includes would be more robust.

---

## 5. API Layer

| Aspect | Rating |
|--------|--------|
| Endpoint design | Good |
| Contract accuracy | Good |
| OpenAPI spec | Good |
| Obsolete endpoints | Needs Improvement |

### Findings

**DocumentsController** properly adapts between generated NSwag contracts and MediatR commands. The download endpoint returns a 302 redirect to the SAS URI — correct per ADR-015.

**GlobalExceptionMiddleware** handles `DomainException` → 400, `ValidationException` → 400 (with field-level errors), `UnauthorizedAccessException` → 403, `KeyNotFoundException` → 404.

> **Minor Finding**: The upload endpoint uses base64-encoded file content in a JSON body (`UploadDocumentRequest.fileContent` as `byte[]`). For files up to 25MB, this causes:
> - 33% size overhead from base64 encoding
> - Large JSON serialization/deserialization costs
> - Memory pressure from byte arrays
>
> A `multipart/form-data` approach with `IFormFile` would be more appropriate for large file uploads.

---

## 6. Blazor Layer

| Aspect | Rating |
|--------|--------|
| Component design | Excellent |
| Separation of concerns | Good |
| Code reuse | Good |
| State management | Good |
| ViewModel pattern | Excellent |

### Findings

**DynamicFormGenerator** is a well-designed reusable component supporting all 6 field types (Text, MultilineText, Number, Date, Boolean, Dropdown, FileUpload) in both Edit and ReadOnly modes. The `ChildContent` RenderFragment allows parent pages to inject buttons while the form handles validation.

**ApplicationEdit** properly integrates document upload/delete flows:

- `HandleFileSelected` — uploads, then refreshes application state
- `HandleDocumentDeleted` — deletes blob, removes from local list
- Error handling differentiates `UnauthorizedAccessException`, `InvalidOperationException`, and generic exceptions

**DocumentRequirementCard** cleanly shows document status (satisfied / missing / not-supplied) with upload/download controls.

**ViewModels** follow a clean pattern with `IsLoading`, `HasError`, `ErrorMessage` state flags.

> **Minor Finding**: `ApplicationEditViewModel.Load()` uses `fd.Name` as the `Label` property — meaning field labels are the raw field name rather than a display-friendly label. The `PermitField` value object doesn't carry a separate display label property.

> **Minor Finding**: The Blazor `Services/` directory is empty, suggesting service registration for the Blazor app happens elsewhere (likely in `Program.cs`). Not a defect, but worth noting for discoverability.

---

## 7. Security

| Aspect | Rating |
|--------|--------|
| Authorization | Excellent |
| Ownership validation | Excellent |
| Blob access security | Excellent |
| Entra ID integration | Excellent |

### Findings

**Ownership validation** is consistently enforced in every command handler:

- `UploadDocumentCommandHandler`: Checks `application.CitizenId != uploadedById`
- `DeleteDocumentCommandHandler`: Checks `application.CitizenId != _currentUserService.UserId`
- `DownloadDocumentQueryHandler`: Role-aware check (Citizen=own, Officer=assigned, Admin=all)

**Blob access** is secured via SAS URIs with 1-hour expiry and 5-minute clock skew buffer. The raw `BlobUrl` is stored in the database but never returned through the download endpoint.

**Authorization policies** flow from OpenAPI spec → `GeneratedControllerAuthorizationConvention` → action filters. Documents require `Authenticated` (any user), which is correct.

**Entra ID** is the single identity provider. User synchronization occurs on every authenticated request via `UserSynchronizationBehavior`. JWT validation includes both v1.0 and v2.0 issuer/audience formats.

---

## 8. Testing

| Aspect | Rating |
|--------|--------|
| Unit test coverage | Excellent |
| Integration test coverage | Good |
| Blazor component tests | Good |
| Transaction / pipeline tests | Needs Improvement |

### Findings

**Unit tests** for document commands are comprehensive:

- `UploadDocumentCommandHandlerTests`: 5+ tests covering happy path, not-found, ownership mismatch, null request, ADR-015 naming
- `DeleteDocumentCommandHandlerTests`: 7+ tests covering all major paths including missing blob warning, ownership rejection, non-draft rejection

**Integration tests** cover the full document lifecycle:

- Upload → return 204
- Upload → Download end-to-end (verify SAS redirect)
- Delete → return 204
- Delete → unauthorized (cross-ownership)

**Blazor component tests** exist for `DynamicFormGenerator` (both standard and file upload variants) using bUnit.

### Gaps

- **TransactionBehavior** has no isolation-level unit test. The existing `TransactionBehaviorTests` tests `UnitOfWork.SaveChangesAsync` but not the behavior itself.
- **Download authorization** has no unit test for the role-based logic (Citizen vs Officer vs Admin).
- **Blazor ApplicationEdit** page has no test for the document upload/delete handlers (only view model tests exist).
- **No performance or load tests** for blob upload/download scenarios.

---

## 9. Documentation

| Aspect | Rating |
|--------|--------|
| ADRs | Excellent |
| Architecture docs | Excellent |
| Milestone plan | Excellent |
| README | Good |

### Findings

**ADR-015** (Document Storage Architecture) is exceptional — covers decision context, alternatives considered (with rejection rationale), blob naming convention, container strategy, and security considerations. It accurately reflects the implementation.

**ADR-014** (Dynamic Permit Form Storage) provides thorough coverage of the FieldValues storage strategy, including the no-separate-repository decision.

**Milestone 6 plan** (`plans/milestone-06-document-management-plan.md`) is comprehensive with phased tasks, LOC estimates, and dependency tracking.

### Gaps

- **No ADR for AR-001** (transaction pipeline refactoring). The pipeline changes are documented in XML comments but not in a formal ADR.
- **No local development guide** for Azurite (blob storage emulator) — the milestone plan mentions it but it's not created.

---

## 10. Technical Debt Register

| # | Finding | Category | Severity |
|---|---------|----------|----------|
| C-01 | `ApplicationAggregate.ValidateInvariants()` is never called | Dead code | Critical |
| C-02 | `SubmitApplicationCommand` is obsolete but still wired in controller | Dead endpoint | Critical |
| M-01 | `DocumentDto.BlobUrl` exposed in API response | Security leak | Major |
| M-02 | Event handlers commit outside `TransactionBehavior` | Consistency gap | Major |
| M-03 | No `IDbContextTransaction` across sync + handler | Partial commit risk | Major |
| M-04 | `AuditLogRepository.UpdateAsync/DeleteAsync` on immutable log | Design inconsistency | Major |
| m-01 | Duplicate FluentValidation rules in `UploadDocumentCommandValidator` | Code quality | Minor |
| m-02 | `ApplicationRepository.GetByIdAsync` missing `.Include()` | Fragility | Minor |
| m-03 | Field labels default to raw field name | UX gap | Minor |
| m-04 | Base64 file upload (not multipart/form-data) | Scalability | Minor |
| m-05 | No ADR for AR-001 pipeline refactoring | Documentation gap | Minor |
| m-06 | No Azurite local development guide | Documentation gap | Minor |
| m-07 | `DownloadDocumentQueryHandler` publishes event without await | Fire-and-forget | Minor |
| m-08 | Blazor `Services/` directory is empty | Discoverability | Minor |

---

## 11. Rating Summary

| Area | Rating |
|------|--------|
| Domain Model | Good |
| Application Layer | Excellent |
| CQRS | Excellent |
| MediatR Pipeline | Good |
| Transaction Management | Good |
| Infrastructure (Blob + EF) | Excellent |
| API | Good |
| Blazor / Component Design | Excellent |
| Security | Excellent |
| Testing | Good |
| Documentation | Excellent |
| Maintainability | Good |
| Extensibility | Good |

---

## 12. Final Assessment

### 12.1 Does Milestone 6 satisfy its architectural objectives?

**YES**

The milestone delivers:

- Complete document upload/delete/replace workflow
- Azure Blob Storage integration through Clean Architecture ports
- Secure download via SAS tokens
- Document requirement validation per permit type
- End-to-end citizen document workflow in Blazor
- Expanded test coverage across all layers
- Transaction pipeline refactoring (AR-001)

The implementation accurately reflects ADR-015 and ADR-014 design decisions.

### 12.2 Is the implementation aligned with Clean Architecture, DDD, CQRS, SOLID?

**YES** — with minor deviations.

| Principle | Assessment |
|-----------|------------|
| **Clean Architecture** | Dependencies flow inward. Domain has zero infrastructure dependencies. Application defines interfaces; Infrastructure implements them. API depends on Application only. ✅ |
| **DDD** | Aggregates, value objects, domain events, and domain exceptions are all correctly implemented. `ValidateInvariants()` is unused — a gap. ✅ mostly |
| **CQRS** | Commands use `ICommand<T>` marker; queries use `IRequest<T>`. `TransactionBehavior` is constrained to commands only. ✅ |
| **SOLID** | Single responsibility ✅, Open for extension ✅, Liskov substitutable ✅, Interface segregation ✅, Dependency inversion ✅ |

**Deviations:**

1. `SubmitApplicationCommand` is `[Obsolete]` but still wired — violates the Open/Closed Principle for the API surface
2. `BlobUrl` leaks through DTO — violates Dependency Inversion (callers depend on storage URLs)
3. `ValidateInvariants()` is dead code — violates the intent of Single Responsibility

### 12.3 Critical Findings

| ID | Finding | Location |
|----|---------|----------|
| **C-01** | `ApplicationAggregate.ValidateInvariants()` is defined but **never called** by any command handler. The invariant checks for rejection reason codes, document-aggregate ownership, and review state consistency provide no protection. | `src/ATLAS.Domain/Aggregates/ApplicationAggregate.cs` |
| **C-02** | `SubmitApplicationCommand` is `[Obsolete]` but still wired in `ApplicationsController.ApplicationsPost()`. The old one-step submission bypasses the draft workflow entirely, creating two inconsistent code paths for application creation. | `src/ATLAS.API/Controllers/ApplicationsController.cs:66`, `src/ATLAS.Application/Commands/Applications/SubmitApplicationCommand.cs:13` |

### 12.4 Major Findings

| ID | Finding | Location |
|----|---------|----------|
| **M-01** | `DocumentDto.BlobUrl` is exposed through the API response. ADR-015 explicitly states BlobUrl is never exposed directly. The download endpoint correctly returns SAS URIs, but the detail endpoint leaks the raw storage URL. | `src/ATLAS.Application/DTOs/ApplicationDtos.cs:43`, `src/ATLAS.Application/Queries/Applications/GetApplicationByIdQuery.cs:96` |
| **M-02** | Event handlers (`DocumentUploadedEventHandler`, `DocumentDownloadedEventHandler`) write audit logs via `IAuditLogRepository` — these commits happen **outside** the `TransactionBehavior` scope. Partial commit risk. | `src/ATLAS.Infrastructure/EventHandlers/DocumentUploadedEventHandler.cs` |
| **M-03** | `UserSynchronizationBehavior` commits independently from `TransactionBehavior` (no `IDbContextTransaction` wrapping). If the handler fails after sync succeeds, user sync changes are persisted. AR-001 acknowledges this but it remains a risk. | `src/ATLAS.Application/Behaviors/UserSynchronizationBehavior.cs:47-53` |
| **M-04** | `AuditLogRepository.UpdateAsync/DeleteAsync` expose mutation operations on an immutable audit log. While currently uncalled, they represent a design inconsistency for a write-once log. | `src/ATLAS.Infrastructure/Repositories/AuditLogRepository.cs:62-73` |

### 12.5 Minor Findings

| ID | Finding | Location |
|----|---------|----------|
| m-01 | Duplicate FluentValidation rules for `FileSize` and `FileContent` in `UploadDocumentCommandValidator` | `src/ATLAS.Application/Validators/CommandValidators.cs` |
| m-02 | `ApplicationRepository.GetByIdAsync()` does not use `.Include()` for owned entities — relies on EF Core default loading behavior | `src/ATLAS.Infrastructure/Repositories/ApplicationRepository.cs:18` |
| m-03 | Field labels default to raw `PermitField.Name` — no separate display label property | `src/ATLAS.Blazor/ViewModels/ApplicationEditViewModel.cs:51` |
| m-04 | Base64-encoded file upload — for 25MB files, causes 33% overhead and high memory pressure | `openapi/atlas-api.yaml` (UploadDocumentRequest schema) |
| m-05 | No ADR exists for AR-001 transaction pipeline refactoring | Documentation |
| m-06 | No Azurite local development guide exists | Documentation |
| m-07 | `DownloadDocumentQueryHandler` publishes `DocumentDownloadedEvent` but does not await the publish — fire-and-forget semantics | `src/ATLAS.Application/Queries/Documents/DownloadDocumentQuery.cs` |
| m-08 | Blazor `Services/` directory is empty — service registration location is unclear | `src/ATLAS.Blazor/Services/` |

### 12.6 Can Milestone 6 be considered production-ready?

**YES** — with remediation of Critical findings.

**Justification:**

The implementation is architecturally sound and functionally complete. The critical findings (C-01, C-02) are contained:

- **C-01** (unused `ValidateInvariants`) is a safety net that isn't currently needed because individual handlers enforce their own invariants. However, it should be integrated before Milestone 7 adds more complex workflows.
- **C-02** (obsolete Submit endpoint) is a co-existence issue — the new draft workflow is the primary path, and the old endpoint can be removed independently.

The major findings (M-01 through M-04) represent consistency gaps rather than correctness defects:

- BlobUrl exposure is a defense-in-depth concern — an attacker would need authenticated access first
- Event handler commits outside `TransactionBehavior` are consistent with the existing pattern
- The sync-before-handler commit window is an acknowledged trade-off

**Remediation required before production deployment:**

1. Remove or unwire `SubmitApplicationCommand` endpoint (C-02)
2. Integrate `ValidateInvariants()` into command handlers (C-01)
3. Remove `BlobUrl` from `DocumentDto` (M-01)

### 12.7 Is Milestone 6 ready to serve as the architectural foundation for Milestone 7?

**GO**

**Rationale:**

The architecture is stable, well-documented, and correctly aligned with the project's chosen patterns (Clean Architecture, DDD, CQRS, MediatR pipeline). No fundamental redesign is required.

Milestone 7 (Officer Workflow, Administration, Notifications, or similar) will build directly on:

- The same aggregate boundaries (Application owns Documents, Reviews, FieldValues)
- The same pipeline behaviors (Validation → UserSync → Transaction → Handler)
- The same blob storage abstraction
- The same authorization conventions
- The same component patterns (ViewModels, DynamicFormGenerator)

**Recommended actions before Milestone 7 begins:**

| Priority | Action | Rationale |
|----------|--------|-----------|
| 🔴 Before M7 | Resolve C-01 (wire `ValidateInvariants()`) | Prevent invariant bypass in new workflows |
| 🔴 Before M7 | Resolve C-02 (remove obsolete endpoint) | Eliminate dual code paths |
| 🟡 Sprint 1 of M7 | Address M-01 (remove BlobUrl from DTO) | Defense-in-depth |
| 🟡 Sprint 1 of M7 | Address M-02, M-03 (transaction consistency) | Improve reliability |
| 🟢 Opportunistic | M-04, m-01 through m-08 | Incremental improvement |

The existing foundation will support officer dashboards, admin panels, notification services, and audit reporting without significant restructuring. The extension points are already identified in the architecture documentation (Service Bus integration, reporting, workflow engine, rich notifications).

**Final Verdict: GO** — proceed to Milestone 7 with the two critical findings resolved at the earliest opportunity.

---

## Appendix A: Files Reviewed

### Domain Layer
- `src/ATLAS.Domain/Aggregates/ApplicationAggregate.cs`
- `src/ATLAS.Domain/Entities/Application.cs`
- `src/ATLAS.Domain/Entities/Document.cs`
- `src/ATLAS.Domain/Entities/Entity.cs`
- `src/ATLAS.Domain/Entities/PermitType.cs`
- `src/ATLAS.Domain/Entities/ApplicationFieldValue.cs`
- `src/ATLAS.Domain/Enums/DocumentType.cs`
- `src/ATLAS.Domain/ValueObjects/DocumentRequirement.cs`
- `src/ATLAS.Domain/Events/DocumentUploadedEvent.cs`
- `src/ATLAS.Domain/Events/DocumentDownloadedEvent.cs`
- `src/ATLAS.Domain/DomainException.cs`
- `src/ATLAS.Domain/Interfaces/IApplicationRepository.cs`

### Application Layer
- `src/ATLAS.Application/Commands/ICommand.cs`
- `src/ATLAS.Application/Commands/Documents/UploadDocumentCommand.cs`
- `src/ATLAS.Application/Commands/Documents/DeleteDocumentCommand.cs`
- `src/ATLAS.Application/Commands/Applications/SubmitApplicationCommand.cs`
- `src/ATLAS.Application/Commands/Applications/SubmitDraftCommand.cs`
- `src/ATLAS.Application/Commands/Applications/UpdateDraftCommand.cs`
- `src/ATLAS.Application/Commands/Applications/CreateDraftCommand.cs`
- `src/ATLAS.Application/Queries/Documents/DownloadDocumentQuery.cs`
- `src/ATLAS.Application/Queries/Applications/GetApplicationByIdQuery.cs`
- `src/ATLAS.Application/Validators/CommandValidators.cs`
- `src/ATLAS.Application/DTOs/ApplicationDtos.cs`
- `src/ATLAS.Application/Interfaces/IFileStorageService.cs`
- `src/ATLAS.Application/Interfaces/IVirusScanner.cs`
- `src/ATLAS.Application/Behaviors/ValidationBehavior.cs`
- `src/ATLAS.Application/Behaviors/UserSynchronizationBehavior.cs`
- `src/ATLAS.Application/Behaviors/TransactionBehavior.cs`

### Infrastructure Layer
- `src/ATLAS.Infrastructure/Data/ApplicationDbContext.cs`
- `src/ATLAS.Infrastructure/Data/UnitOfWork.cs`
- `src/ATLAS.Infrastructure/Data/Configurations/ApplicationConfiguration.cs`
- `src/ATLAS.Infrastructure/Repositories/ApplicationRepository.cs`
- `src/ATLAS.Infrastructure/Repositories/AuditLogRepository.cs`
- `src/ATLAS.Infrastructure/Services/BlobStorageService.cs`
- `src/ATLAS.Infrastructure/Services/InMemoryFileStorageService.cs`
- `src/ATLAS.Infrastructure/Services/PassThroughVirusScanner.cs`
- `src/ATLAS.Infrastructure/Options/StorageOptions.cs`
- `src/ATLAS.Infrastructure/ServiceCollectionExtensions.cs`
- `src/ATLAS.Infrastructure/EventHandlers/DocumentUploadedEventHandler.cs`
- `src/ATLAS.Infrastructure/EventHandlers/DocumentDownloadedEventHandler.cs`

### API Layer
- `src/ATLAS.API/Program.cs`
- `src/ATLAS.API/Controllers/DocumentsController.cs`
- `src/ATLAS.API/Controllers/ApplicationsController.cs`
- `src/ATLAS.API/Auth/GeneratedControllerAuthorizationConvention.cs`
- `src/ATLAS.API/Middleware/GlobalExceptionMiddleware.cs`
- `openapi/atlas-api.yaml`

### Blazor Layer
- `src/ATLAS.Blazor/Components/Pages/ApplicationCreate.razor`
- `src/ATLAS.Blazor/Components/Pages/ApplicationEdit.razor`
- `src/ATLAS.Blazor/Components/Pages/ApplicationEdit.razor.cs`
- `src/ATLAS.Blazor/Components/Shared/DynamicFormGenerator.razor`
- `src/ATLAS.Blazor/Components/Shared/DynamicFormGenerator.razor.cs`
- `src/ATLAS.Blazor/Components/Shared/DocumentRequirementCard.razor`
- `src/ATLAS.Blazor/ViewModels/ApplicationEditViewModel.cs`

### Tests
- `tests/ATLAS.Domain.Tests/Entities/DocumentTests.cs`
- `tests/ATLAS.Application.Tests/Commands/UploadDocumentCommandHandlerTests.cs`
- `tests/ATLAS.Application.Tests/Commands/DeleteDocumentCommandHandlerTests.cs`
- `tests/ATLAS.Infrastructure.Tests/Repositories/TransactionBehaviorTests.cs`
- `tests/ATLAS.Infrastructure.Tests/Services/InMemoryFileStorageServiceTests.cs`
- `tests/ATLAS.IntegrationTests/API/DocumentsControllerTests.cs`
- `tests/ATLAS.Blazor.Tests/Components/Shared/DynamicFormGeneratorTests.cs`

### Documentation
- `docs/ADRs/adr-014-dynamic-permit-form-storage.md`
- `docs/ADRs/adr-015-document-storage-architecture.md`
- `plans/milestone-06-document-management-plan.md`

---

*Review completed 2026-06-29. All findings verified against implementation code.*