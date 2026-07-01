---
title: "ADR-015: Document Storage Architecture"
status: "Accepted"
date: "2026-06-23"
authors: "Engineering Team"
tags: ["architecture", "storage", "documents", "security", "blob-storage"]
supersedes: ""
superseded_by: ""
---

# ADR-015: Document Storage Architecture

## Status

### Accepted

## Context

Milestone 6 (Document Management) requires a complete document storage solution for citizen-uploaded supporting documents. The PRD specifies:

- **F-03**: Citizens can upload supporting documents (PDF, JPG, PNG) up to 25MB per file
- **F-08**: Citizens can download previously uploaded documents via secure SAS token (1-hour expiry)
- **F-10**: Officers can view uploaded documents during application review
- **C-04**: Document storage must use Azure Blob Storage
- **NFR-16**: Geo-redundant storage (GRS) for Azure Blob Storage
- **NFR-19**: Azure Blob Storage supports unlimited document growth

The existing codebase already contains:

- A Document entity as a child of the Application aggregate (metadata only)
- A DocumentUploadedEvent domain event
- A DocumentRequirement value object on PermitType
- An UploadDocumentCommand + handler (expects pre-uploaded BlobUrl)
- A stub download endpoint returning 501
- An Application.AddDocument() method with status gating
- EF Core configuration for Document metadata persistence

Key architectural constraints:

- Binary content goes to Azure Blob Storage, metadata stays in SQL Server
- All blob access must be secured -- no publicly accessible blobs
- Local development must work without Azure (Azurite)
- No functional code changes during D0 -- this ADR documents the design decisions only

## Decision

### 1. File Storage Abstraction

Define an IFileStorageService interface in the Application layer to preserve Clean Architecture dependency rules:

```csharp
public interface IFileStorageService
{
    Task<FileUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<FileDownloadResult?> DownloadAsync(string blobUrl, CancellationToken ct = default);
    Task<string> GenerateDownloadSasUriAsync(string blobUrl, TimeSpan expiry, CancellationToken ct = default);
    Task<bool> DeleteAsync(string blobUrl, CancellationToken ct = default);
}

public record FileUploadResult(string BlobUrl, long Size);
public record FileDownloadResult(Stream Content, string ContentType, string FileName);
```

- UploadAsync accepts a Stream -- avoids coupling to HttpContext/IFormFile
- GenerateDownloadSasUriAsync creates time-limited SAS URIs per PRD F-08
- DownloadAsync is retained as a fallback for SAS generation failures
- Infrastructure layer provides BlobStorageService (production) and InMemoryFileStorageService (test double)

### 2. Container Strategy

| Container | Purpose | Access Tier |
| ----------- | --------- | ------------- |
| permit-documents | All uploaded application documents | Hot |
| permit-documents-deleted | Soft-deleted documents (future use) | Cool |

Single container with folder-style virtual paths. Container access level is private -- no public blob access.

### 3. Blob Naming Convention

`
{applicationId}/{documentId}/{fileName}
`

Example: 1b2c3d4-.../e5f6g7h8-.../site-plan.pdf

Rationale:

- ApplicationId prefix groups documents logically for listing
- DocumentId prefix prevents name collisions across uploads
- GUID prefixes eliminate path traversal risk
- Blob URL is the single persisted storage reference; no separate BlobPath property is stored

### 4. Metadata Persistence

The existing Document entity uses BlobUrl as the single persisted storage reference. No BlobPath property is introduced:

| Property | Type | Purpose |
| ---------- | ------ | --------- |
| Id | Guid | Primary key |
| ApplicationId | Guid | Parent aggregate |
| FileName | string | Original user-friendly filename |
| ContentType | string | MIME type |
| FileSize | long | Size in bytes |
| BlobUrl | string | Full URL to blob (single storage reference) |
| UploadedDate | DateTime | When uploaded |
| UploadedById | Guid | Who uploaded |

### 5. Security Model

**Upload authorization:**

| Role | Can Upload | Scope |
| ------ | ------------ | ------- |
| Citizen | Yes | Own applications only |
| Officer | No | Review-only, no upload |
| Admin | No | Configuration-only, no upload |

Enforcement: Handler checks currentUser.UserId == application.CitizenId. Application status must not be Approved/Rejected (enforced by domain).

**Download authorization (SAS token model):**

| Role | Can Download | Scope | Method |
| ------ | -------------- | ------- | -------- |
| Citizen | Yes | Own applications only | SAS token (1-hour) |
| Officer | Yes | Assigned or unassigned applications | SAS token (1-hour) |
| Admin | Yes | All applications | SAS token (1-hour) |

**Blob access model:**

- Blobs are never publicly accessible (container = private)
- Download endpoint generates SAS URIs with 1-hour expiry, read-only permission
- Client receives a 302 redirect to the SAS URI or a JSON response with the URI
- If SAS generation fails, the download endpoint degrades to app-proxied streaming
- No SAS tokens for document listing or management -- only for download

### 6. Validation Strategy

**Allowed file types:** PDF (pplication/pdf), JPEG (image/jpeg), PNG (image/png) -- per PRD F-03.

**Size limit:** 25MB maximum per file -- enforced by the Document entity constructor as the single source of truth. The existing Application-layer validator (currently 10MB) must be aligned.

**Validation layers:**

1. Client-side: HTML ccept attribute on input type="file" for browser filtering
2. Server-side validator: FluentValidation checks content type and size
3. Domain entity constructor: Enforces final size limit and non-empty validation
4. DocumentRequirement: Per-permit-type validation (file type and size override)

**Future extension point:** IVirusScanService interface added in D7 with a pass-through implementation for MVP. Post-MVP integration with Microsoft Defender for Storage.

### 7. Local Development Strategy

**Azurite** (Azure Storage Emulator):

- Ships with Visual Studio 2022+
- Configuration via Storage:ConnectionString = "UseDevelopmentStorage=true"
- Container auto-created on first use
- SAS token generation works in Azurite for local testing

**appsettings.Development.json addition:**

`json
{
  "Storage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "permit-documents",
    "SasTokenExpiryHours": 1
  }
}
`

**Test strategy:**

- Unit/integration tests use InMemoryFileStorageService -- no Azurite dependency
- Optional integration smoke tests against Azurite via test configuration flag

## Consequences

### Positive

- **POS-001**: Clean separation of concerns -- IFileStorageService in Application layer preserves Clean Architecture rules
- **POS-002**: Blob naming with GUIDs eliminates path traversal and collision risks
- **POS-003**: SAS tokens provide direct, scalable downloads without proxying through app server
- **POS-004**: BlobUrl is the single persisted storage reference, eliminating redundant blob path metadata
- **POS-005**: Azurite provides realistic local development without Azure dependency

### Negative

- **NEG-001**: Two-phase upload (blob storage then metadata persistence) adds complexity to the upload handler
- **NEG-002**: SAS token expiry (1 hour) requires clients to handle token refresh for long-running views
- **NEG-003**: DocumentRequirement enforcement adds a validation dependency on PermitType loading during upload

## Alternatives Considered

### App-Proxied Download (no SAS tokens)

- **ALT-001**: **Description**: All document downloads stream through the API endpoint
- **ALT-002**: **Rejection Reason**: Simpler but less scalable -- app server becomes a bottleneck for concurrent downloads. PRD F-08 explicitly specifies SAS tokens.

### Single SQL VARBINARY Storage

- **ALT-003**: **Description**: Store binary content directly in SQL Server as VARBINARY(MAX)
- **ALT-004**: **Rejection Reason**: Rejected per ADR-003 -- large blobs degrade backup/restore performance and increase database cost. Azure Blob Storage provides unlimited scale at lower cost.

## Implementation Notes

- **IMP-001**: The existing UploadDocumentCommand expects a pre-uploaded BlobUrl string -- this must be rewritten to accept a file stream and call IFileStorageService.UploadAsync() internally
- **IMP-002**: The existing DocumentsController.Download() stub returning 501 must be replaced with the SAS token generation flow
- **IMP-003**: The DocumentType enum (PDF=1, JPG=2, PNG=3) is unused dead code -- deprecate in D1
- **IMP-004**: The existing size limit mismatch (domain=25MB, validator=10MB) must be resolved by aligning the validator with the domain entity
- **IMP-005**: The duplicate DocumentUploadedEvent publication (raised by aggregate AND by handler) must be fixed in D3
- **IMP-006**: Infrastructure-as-Code (Bicep for Storage Account and container) is planned in a separate workstream

## References

- **REF-001**: ADR-003 -- Azure SQL & Blob Storage (overall storage strategy)
- **REF-002**: ADR-004 -- Domain-Driven Design (Document entity, Application aggregate)
- **REF-003**: ADR-011 -- Data Lifecycle Management (blob retention policies)
- **REF-004**: PRD F-03, F-08, F-10, C-04, NFR-16, NFR-19
- **REF-005**: Milestone 6 Phase Plan (plans/milestone-06-document-management-plan.md)
