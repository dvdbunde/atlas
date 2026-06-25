# Milestone 6: Document Management

**Project**: ATLAS (Automated Tracking & Licensing Application System)
**Version**: 1.0
**Date**: June 23, 2026
**Status**: ![In Progress](https://img.shields.io/badge/status-In%20Progress-blue)

## Objective

Deliver complete document management for citizen permit applications: upload supporting documents, store binary content in Azure Blob Storage with metadata in SQL Server, provide secure download via SAS tokens, and enforce document requirements per permit type.

## Dependencies

| Phase | Dependencies | Description |
| ------- | ------------- | ------------- |
| D1 | None | Domain & Storage Abstractions — can start immediately |
| D2 | D1 | Azure Blob Storage Integration — needs IFileStorageService interface |
| D3 | D2 | Upload Backend — needs blob storage service implementation |
| D4 | D1 | FileUpload Field Type — needs FieldType.FileUpload enum value |
| D5 | D3, D4 | Citizen Upload Experience — needs upload backend and FileUpload field |
| D6 | D2, D5 | Download & Document Viewing — needs blob storage reads and documents to download |
| D7 | D3, D5, D6 | Validation & Security Hardening — needs all functional code in place |
| D8 | D1–D7 | Testing & Documentation — integration pass after all phases |

## Phase Plan

### D1 — Domain & Storage Abstractions

**Est. LOC**: ~150 → ~100 | **Est. Effort**: 1 session

- Add IFileStorageService interface with FileUploadResult/FileDownloadResult records to src/ATLAS.Application/Interfaces/
- Document entity already has BlobUrl — no new storage property needed
- Add FieldType.FileUpload = 6 to FieldType enum
- Add DocumentDownloadedEvent to src/ATLAS.Domain/Events/
- Deprecate DocumentType enum with [Obsolete] attribute
- Create tests/ATLAS.Domain.Tests/Events/DocumentDownloadedEventTests.cs

### D2 — Azure Blob Storage Integration

**Est. LOC**: ~350 | **Est. Effort**: 1–2 sessions

- Add Azure.Storage.Blobs NuGet package to ATLAS.Infrastructure.csproj
- Implement BlobStorageService in Infrastructure/Services/:
  - UploadAsync — upload stream to blob container
  - DownloadAsync — stream blob content back (for SAS fallback)
  - GenerateDownloadSasUriAsync — create BlobSasBuilder with 1-hour expiry, read permissions
  - DeleteAsync — remove blob
- Implement InMemoryFileStorageService test double
- Configure DI registration in ServiceCollectionExtensions.cs
- Add Storage:ConnectionString, Storage:ContainerName, Storage:SasTokenExpiryHours to appsettings
- Create tests/ATLAS.Infrastructure.Tests/Services/BlobStorageServiceTests.cs
- Create tests/ATLAS.Infrastructure.Tests/Services/InMemoryFileStorageServiceTests.cs
- Document Azurite setup in docs/engineering/local-development.md

### D3 — Upload Backend

**Est. LOC**: ~350 | **Est. Effort**: 1 session

- Rewrite UploadDocumentCommand to accept Stream file content
- Rewrite UploadDocumentCommandHandler to call IFileStorageService.UploadAsync() then persist metadata
- Fix UploadDocumentCommandValidator — align 10MB→25MB with domain entity
- Add DocumentRequirement enforcement — validate file type/size against permit type's requirements
- Add ownership validation — verify currentUser.UserId == application.CitizenId
- Fix duplicate event publishing — remove redundant _mediator.Publish() call
- Update UploadDocumentRequest contract and OpenAPI spec for binary body
- Regenerate NSwag controllers/contracts
- Update DocumentsController to wire file stream from request to command
- Update all handler/validator/controller tests

### D4 — FileUpload Field Type

**Est. LOC**: ~200 | **Est. Effort**: 1 session

- Add FileUpload case to DynamicFormGenerator.razor switch — render InputFile component
- Add FileUpload case for ReadOnly mode — render download links
- Update DynamicFormFieldViewModel — add document upload state
- Update DynamicFieldValidator — FileUpload validation rules
- Update FieldDefinitionDto mapping to expose document requirement metadata
- Update PermitTypeDto to include document requirements
- Consume the FileUpload field definitions already exposed by the Application layer.
- Create tests/ATLAS.Blazor.Tests/Components/DynamicFormGeneratorFileUploadTests.cs

### D5 — Citizen Upload Experience

**Est. LOC**: ~500 | **Est. Effort**: 2–3 sessions

- Update ApplicationEdit.razor — integrate FileUpload fields with backend upload command
- Add upload progress indicator (Bootstrap progress bar or spinner)
- Add drag-and-drop zone styling for file upload area
- Handle upload errors (size exceeded, wrong type, network failure)
- Show uploaded document list with file names, sizes, upload dates, and download buttons
- Update ApplicationDetail.razor — render uploaded documents list with download links
- Update ApplicationDetailViewModel — add document field display
- Add confirmation step in submit flow — warn if required documents are missing
- Create tests for edit page and detail page document features
- Manual QA: verify upload flow end-to-end with Azurite

### D6 — Download & Document Viewing

**Est. LOC**: ~250 | **Est. Effort**: 1 session

- Implement DownloadDocumentQuery + handler — authorize → load metadata → generate SAS URI
- Update DocumentsController.Download() — return 302 redirect to SAS URI
- Implement SAS generation in BlobStorageService — 1-hour expiry, read-only
- Handle authorization failure (403), not found (404), SAS failure (fallback to app-proxied stream)
- Wire Blazor download button to API endpoint
- Create query handler tests and controller tests
- Manual QA: download previously uploaded files

### D7 — Validation & Security Hardening

**Est. LOC**: ~200 | **Est. Effort**: 1 session

- Resolve size limit inconsistency — single source of truth: domain = 25MB
- Add IVirusScanService interface with pass-through MVP implementation
- Add DocumentDownloadedEvent audit logging handler
- Enforce upload authorization: citizen → own applications only
- Enforce download authorization: citizen→own, officer→assigned, admin→all
- Add rate limiting on upload endpoint
- Verify blob container is private
- Create security-focused test cases: wrong user, wrong role, oversized file, wrong type
- Manual security review: verify ownership enforcement end-to-end

### D8 — Testing & Documentation

**Est. LOC**: ~400 new tests | **Est. Effort**: 1–2 sessions

- Full test pass across all 8 phases
- Coverage targets: Domain ≥ 90%, Application ≥ 85%, Infrastructure ≥ 80%, Blazor ≥ 70%
- Integration tests: document upload → persistence → download cycle via test infrastructure
- Update docs/engineering/code-review-guidelines.md — document review checklist for file storage
- Update docs/design/07-data-flow.md — add document upload/download data flow diagrams
- Update docs/engineering/local-development.md — Azurite setup guide
- Update ROADMAP.md — mark M6 complete when delivered

## Summary Table

| Phase | Name | Est. LOC | Est. Sessions | Dependencies |
| ------- | ------ | ---------- | --------------- | ------------- |
| D1 | Domain & Storage Abstractions | 150 | 1 | None |
| D2 | Azure Blob Storage Integration | 350 | 1–2 | D1 |
| D3 | Upload Backend | 350 | 1 | D2 |
| D4 | FileUpload Field Type | 200 | 1 | D1 |
| D5 | Citizen Upload Experience | 500 | 2–3 | D3, D4 |
| D6 | Download & Document Viewing | 250 | 1 | D2, D5 |
| D7 | Validation & Security Hardening | 200 | 1 | D3, D5, D6 |
| D8 | Testing & Documentation | 400 | 1–2 | D1–D7 |
| **Total** | | **~2,400** | **9–14** | |

## Dependency Graph

`mermaid
flowchart TD
    D1[D1: Domain & Abstractions] --> D2[D2: Azure Blob Integration]
    D1 --> D4[D4: FileUpload Field Type]
    D2 --> D3[D3: Upload Backend]
    D3 --> D5[D5: Citizen Upload Experience]
    D4 --> D5
    D2 --> D6[D6: Download & Viewing]
    D5 --> D6
    D3 --> D7[D7: Validation & Security]
    D5 --> D7
    D6 --> D7
    D1 --> D8[D8: Testing & Documentation]
    D2 --> D8
    D3 --> D8
    D4 --> D8
    D5 --> D8
    D6 --> D8
    D7 --> D8
`

## Key References

- **ADR-003**: Azure SQL & Blob Storage (overall storage strategy)
- **ADR-004**: Domain-Driven Design (Document entity, Application aggregate)
- **ADR-015**: Document Storage Architecture (this milestone's design decisions)
- **PRD**: F-03 (upload), F-08 (download), F-10 (officer view), C-04 (Azure Blob constraint)
