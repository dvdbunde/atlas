# Project Roadmap

Use this roadmap to communicate upcoming milestones and recently completed work. Keep it lightweight and updated as plans evolve.

## Status Badges

- Planned: ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- In Progress: ![In Progress](https://img.shields.io/badge/status-In%20Progress-blue)
- Done: ![Done](https://img.shields.io/badge/status-Done-brightgreen)

Usage: Add one badge per item to convey current status.

---

## Milestones (Completed)

All milestones M1 through M8 have been implemented and merged as of July 2026.

| Milestone | Description | Status |
| --------- | ----------- | ------ |
| **M1: Solution Foundation** | Clean Architecture scaffolded with .NET 9, CI/CD pipelines (GitHub Actions), solution structure | ✅ Complete |
| **M2: Domain Model** | Core entities, aggregates, value objects, and domain events (ADR-004) | ✅ Complete |
| **M3: Database Persistence** | EF Core with SQL Server (LocalDB), repository pattern, data migrations, UnitOfWork | ✅ Complete |
| **M4: Authentication** | Microsoft Entra ID integration with role-based authorization (ADR-008, ADR-013) | ✅ Complete |
| **M5: Permit Submission** | Draft applications, dynamic forms (ADR-014), citizen dashboard, application detail/edit, confirmation workflow, email notifications | ✅ Complete |
| **M6: Document Management** | Azure Blob Storage integration (Azurite for local dev), upload/download, SAS token security, FileUpload field type, InMemoryFileStorageService test double (ADR-015) | ✅ Complete |
| **M7: Officer Review** | Officer dashboard, application review, approve/reject/request-info workflow, internal notes, search/filter, activity timeline, citizen info-request response (ADR-017) | ✅ Complete |
| **M8: Administration** | Admin dashboard, permit type management (designer with field/document-requirement editing), User Directory (read-only, Entra-synchronized), Audit Log viewer, Email Template administration, Application Explorer, shared Application Details layout (ADR-018) | ✅ Complete |

### Detailed Scope per Milestone

**M6 — Document Management (D1–D8 phases):**

- `IFileStorageService` abstraction in Application layer
- `BlobStorageService` (Azure SDK) and `InMemoryFileStorageService` (test double) implementations
- `UploadDocumentCommand` rewritten for stream-based binary upload with validation
- `FileUpload` FieldType for dynamic forms (`FieldType.FileUpload`)
- SAS token generation for secure downloads (1-hour expiry)
- Document requirement enforcement (file type/size vs permit type config)

**M7 — Officer Review Workflow:**

- Officer dashboard (`/officer/dashboard`) with status-filtered application queue
- Application review page with full detail view, document access, review history
- Approve/Reject/Request-Information workflow actions
- Internal review notes (officer-only visibility)
- Activity Timeline component replacing earlier Lifecycle presentation
- Citizen info-request response (resubmit workflow; ADR-017)

**M8 — Administration Portal (A1–A7 phases):**

- Admin shell with role-based navigation, `PageHeader`/`EmptyState` shared components
- Permit type list, detail, designer (fields, document requirements, live preview)
- User Directory (read-only, Entra-synchronized) — list + detail views; no role editing
- Audit Log viewer — filterable read-only view over `GetAuditLogsQuery`
- Email Template administration — list/edit rendered templates
- Application Explorer — read-only admin view of all applications
- A6 (Reference Data & System Settings) rescoped to conformance note only (no new subsystem needed)
- Dynamic Forms page — retained as `EmptyState` placeholder (superseded by PermitType Designer)

---

## Milestone 9 — Cloud Infrastructure & Azure Enablement (Next)

**Status**: ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

**Goal**: Deploy ATLAS to Azure infrastructure, enabling production hosting, managed services, and secure operations.

**Key Deliverables (planned):**

- Azure App Service hosting for Blazor and API
- Azure SQL Database (migrate from LocalDB)
- Azure Blob Storage (production container with SAS security)
- Azure Key Vault integration for secrets management (ADR-009)
- Bicep infrastructure templates (ADR-007)
- Deployment pipelines (CI/CD to Azure)
- Production readiness: custom domain, SSL, monitoring
- Infrastructure documentation and runbook

**Dependencies**: Milestones 1–8 (application implementation complete)

---

## Post-MVP — Phase 2 & 3 (Future)

### Phase 2: Enhanced Features

**Goals**: Add notifications, workflow engine, and advanced features based on MVP feedback.

- Notification Service (Multi-channel: Email, SMS) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Workflow Engine (Custom Approval Chains) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Azure Service Bus Integration (Async Processing) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Reporting & Analytics Dashboard ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Azure SQL Row-Level Security (ADI-010) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Audit export to CSV/Excel (F-23) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

### Phase 3: Public Sector Compliance & Scale

**Goals**: Enhance security, compliance, and scalability for broader adoption.

- Azure Key Vault CMK (Customer-Managed Keys) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Entra External ID (Citizen MFA) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- AKS Migration (Container Orchestration) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Data Lifecycle Management & Retention Policy (ADR-011) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- FedRAMP Compliance Documentation ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

---

## References

- **Product Requirements**: `docs/PRDs/atlas-mvp-prd.md`
- **Architecture Decisions**: `docs/ADRs/` (ADR-001 through ADR-018)
- **Current Architecture Snapshot**: `docs/architecture/current-state.md`
- **Future Extension Points**: `docs/design/08-extension-points.md`
