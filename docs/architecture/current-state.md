# ATLAS Current State Architecture — Post Milestone 8.1

**Date**: July 27, 2026
**Purpose**: Establish a clear architectural baseline before Milestone 9 (Cloud Infrastructure & Azure Enablement)

---

## 1. Overview

ATLAS (Automated Tracking & Licensing Application System) is a permit processing platform for local government. After completing Milestones 1–8, the application is fully functional as a local development environment. All core business logic, UI, and infrastructure abstractions are implemented. Azure deployment is planned as the next milestone.

**Key facts:**

- **Status**: All MVP features implemented and tested locally
- **Deployment**: Local development environment only (Kestrel, LocalDB, Azurite)
- **Authentication**: Microsoft Entra ID (all user types)
- **Storage**: LocalDB (SQL Server), Azurite (Blob Storage), SMTP (email)

---

## 2. Implemented Technology Stack

| Component | Technology | Version | Notes |
| --------- | ---------- | ------- | ----- |
| **Runtime** | .NET | 9.0.314 | Configured via `global.json` |
| **Backend Framework** | ASP.NET Core | 9.0 | Minimal API + Controllers |
| **UI Framework** | Blazor Server | 9.0 | Interactive Server rendering |
| **API Documentation** | Swashbuckle + NSwag | 14.1.0 | OpenAPI spec → generated controllers |
| **CQRS/Mediator** | MediatR | 14.1.0 | Commands, Queries, Pipeline Behaviors |
| **Validation** | FluentValidation | 11.11.0 | Command/Query validation |
| **ORM** | Entity Framework Core | 9.0 | Code-first, migrations |
| **Database** | SQL Server (LocalDB) | Local | Production target: Azure SQL |
| **Blob Storage** | Azure.Storage.Blobs + Azurite | 12.24 | Production target: Azure Blob Storage |
| **Authentication** | Microsoft Entra ID | — | JWT Bearer, OAuth 2.0 |
| **Auth Libraries** | Microsoft.Identity.Web | 4.10.0 | Blazor auth components |
| **CI/CD** | GitHub Actions | — | Build, test, security scanning |
| **Secret Scanning** | GitGuardian | — | CI pipeline |
| **Dependency Scanning** | Snyk | — | CI pipeline |

---

## 3. Solution Architecture

### Layer Structure

```txt
ATLAS.slnx
├── src/
│   ├── ATLAS.Domain/          — Entities, aggregates, value objects, domain events
│   ├── ATLAS.Application/     — CQRS commands/queries, DTOs, interfaces, pipeline behaviors
│   ├── ATLAS.Infrastructure/  — EF Core DbContext, repositories, blob storage, email, event handlers
│   ├── ATLAS.API/             — ASP.NET Core API host, NSwag-generated controllers, auth config
│   └── ATLAS.Blazor/          — Blazor Server app, pages, components, view models
└── tests/
    ├── ATLAS.Domain.Tests/
    ├── ATLAS.Application.Tests/
    ├── ATLAS.Infrastructure.Tests/
    ├── ATLAS.API.Tests/
    ├── ATLAS.Blazor.Tests/
    └── ATLAS.IntegrationTests/
```

### Dependency Flow

```txt
Domain (no deps) ← Application (Domain only)
    ← Infrastructure (Domain + Application)
    ← API (all layers) | Blazor (all layers)
```

### Key Behaviors

| Behavior | Implementation |
| -------- | -------------- |
| **Validation Pipeline** | `ValidationBehavior<TRequest, TResponse>` — validates all commands/queries |
| **User Sync Pipeline** | `UserSynchronizationBehavior<TRequest, TResponse>` — syncs Entra ID claims → DB on every authenticated request |
| **Transaction Pipeline** | `TransactionBehavior<TRequest, TResponse>` — auto-commits `IUnitOfWork` for commands only (ADR-016) |
| **Domain Events** | Raised via `Entity.AddDomainEvent()`, published via `UnitOfWork` after save |
| **Authorization** | Convention-based `GeneratedControllerAuthorizationConvention` on API controllers; `[Authorize]` + `AuthorizeView` in Blazor |

---

## 4. Authentication

| Aspect | Implementation |
| ------ | -------------- |
| **Identity Provider** | Microsoft Entra ID (single tenant) |
| **API Auth** | JWT Bearer token validation |
| **Blazor Auth** | OpenID Connect (Microsoft.Identity.Web) |
| **Roles** | Citizen, Officer, Admin (Entra ID app roles) |
| **User Sync** | `UserSynchronizationBehavior` syncs claims → `User` aggregate on every authenticated request |
| **Local User Model** | Read-only Entra ID projection (ADR-013) — no role editing, no account creation |

---

## 5. Current Persistence Model

| Data | Storage | Implementation |
| ---- | ------- | -------------- |
| **Relational data** | LocalDB | EF Core DbContext, code-first migrations |
| **Documents** | Azurite (Blob Storage emulator) | `BlobStorageService` via Azure.Storage.Blobs SDK |
| **Email templates** | File system | `Templates/Emails/*.txt`, loaded by `EmailTemplateRenderer` |
| **Seed data** | JSON file | `PermitTypes.json` on startup |

### Database Schema (EF Core)

Tables: `Users`, `Applications`, `PermitTypes`, `PermitFields`, `DocumentRequirements`, `Documents`, `Reviews`, `ApplicationFieldValues`, `AuditLogs`

---

## 6. Current Personas

| Persona | Role | Portal | Capabilities |
| ------- | ---- | ------ | ------------ |
| **Citizen** | `Citizen` | `/` | Browse permit types, create/edit/submit applications, upload documents, track status, respond to info requests |
| **Permit Officer** | `Officer` | `/officer/*` | Dashboard with search/filter, review applications, approve/reject/request-info, internal notes |
| **Administrator** | `Admin` | `/admin/*` | Dashboard, permit type management (designer), user directory (read-only), audit log viewer, email template admin, application explorer |

---

## 7. Implemented Modules

### Core Domain (M1-M2)

- Clean Architecture solution structure
- Full domain model with DDD: `Application`, `PermitType`, `User`, `Document`, `Review`, `AuditLog`
- Rich state machine on `Application` (Draft → Submitted → UnderReview → Approved/Rejected/InfoRequested → Resubmitted)
- 15+ domain events

### Database & Persistence (M3)

- EF Core DbContext with full entity configurations
- Repository pattern with `IApplicationRepository`, `IPermitTypeRepository`, `IUserRepository`, `IAuditLogRepository`
- `IUnitOfWork` for transactional integrity
- Code-first migrations

### Authentication (M4)

- Microsoft Entra ID integration
- JWT Bearer for API, OpenID Connect for Blazor
- Role-based authorization (Citizen, Officer, Admin)
- Claims → User sync via pipeline behavior

### API Layer (M4, ADR-012)

- OpenAPI-first: `openapi/atlas-api.yaml` is source of truth
- NSwag-generated controllers from OpenAPI spec
- Convention-based authorization on generated controllers

### Permit Submission (M5)

- Dynamic form generation based on permit type configuration (ADR-014)
- Draft save/resume
- Application field values storage (JSON serialized)
- Status history tracking
- Email notifications via SMTP + `EmailTemplateRenderer`

### Document Management (M6, ADR-015)

- `IFileStorageService` abstraction
- `BlobStorageService` (Azure SDK) + `InMemoryFileStorageService` (test double)
- Stream-based binary upload with size/type validation
- SAS token generation for secure download (1-hour expiry)
- `FileUpload` field type in dynamic forms

### Officer Review Workflow (M7)

- Officer dashboard with status/date/permit-type filtering
- Application review page with shared `ApplicationDetailsLayout` (ADR-018)
- Approve/Reject/Request-Information workflow
- Internal review notes
- `ApplicationTimeline` component
- Citizen info-request response and resubmit (ADR-017)

### Administration Portal (M8)

- Admin shell with role-based nav, `PageHeader`/`EmptyState` components
- **Permit Type Designer**: create/edit permit types, add/remove fields and document requirements, live preview
- **User Directory**: read-only list + detail (Entra-synchronized, no writes)
- **Audit Log Viewer**: read-only, filterable (user/action/date/entity)
- **Email Template Administration**: list/edit rendered email templates
- **Application Explorer**: read-only admin view of all applications
- **Placeholder pages** (no business logic): Dynamic Forms (superseded), Reference Data, System Settings, Officers

---

## 8. Major Architectural Decisions (Implemented)

| Decision | ADR | Summary |
| -------- | --- | ------- |
| Clean Architecture | ADR-001 | Four-layer separation with inward dependency rule |
| CQRS with MediatR | ADR-002 | Commands and Queries separated via MediatR |
| Hybrid Storage | ADR-003 | SQL for relational, Blob for documents |
| Domain-Driven Design | ADR-004 | Rich domain model with aggregates and domain events |
| Blazor Server | ADR-005 | Interactive Server rendering for real-time UI |
| GitHub Actions CI | ADR-006 | CI build, test, security scanning |
| Bicep IaC | ADR-007 | Accepted but deferred to Milestone 9 |
| Microsoft Entra ID | ADR-008 | Single identity provider for all users |
| Azure Key Vault | ADR-009 | Packages installed, integration deferred to M9 |
| Row-Level Security | ADR-010 | App-layer filtering implemented; RLS deferred |
| Data Lifecycle Mgmt | ADR-011 | Not yet implemented (proposed) |
| NSwag API Generation | ADR-012 | OpenAPI → generated controllers |
| Entra ID as SSOT | ADR-013 | User aggregate is read-only Entra projection |
| Dynamic Form Storage | ADR-014 | JSON-serialized field values on Application aggregate |
| Document Storage | ADR-015 | Blob storage with SAS tokens, `IFileStorageService` abstraction |
| Transaction Pipeline | ADR-016 | MediatR behavior auto-commits UnitOfWork for commands |
| Citizen Edit on InfoRequest | ADR-017 | Full application editing during info-request cycle |
| Shared App Details Layout | ADR-018 | RenderFragment composition for multi-persona pages |

---

## 9. Known Future Evolution

Milestone 9 will focus on Azure infrastructure enablement:

| Area | Current State | Target State (Post-M9) |
| ---- | ------------- | ---------------------- |
| **Hosting** | Kestrel (localhost) | Azure App Service |
| **Database** | LocalDB | Azure SQL Database (Serverless) |
| **Blob Storage** | Azurite emulator | Azure Blob Storage (GRS) |
| **Secrets** | `appsettings.json` / user-secrets | Azure Key Vault (ADR-009) |
| **Infrastructure** | Manual setup | Bicep templates (ADR-007) |
| **Deployment** | Manual | CI/CD to Azure |
| **Domain** | localhost | Custom domain with SSL |
| **Monitoring** | Console logging | Application Insights |

Post-MVP phases may include:

- Multi-channel notifications (SMS, email batching)
- Custom workflow engine (approval chains)
- Azure Service Bus for async processing
- Reporting and analytics dashboard
- Row-Level Security (ADR-010)
- Data lifecycle management and retention policies (ADR-011)
- Audit CSV export (F-23)

---

## 10. Design Principles

The current implementation intentionally emphasizes:

• Clean Architecture
• Domain-Driven Design
• CQRS
• API-first development
• OpenAPI contract generation
• Testability
• Infrastructure abstraction
• Cloud readiness

## 11. References

- **README**: `README.md`
- **PRD**: `docs/PRDs/atlas-mvp-prd.md`
- **ADRs**: `docs/ADRs/` (ADR-001 through ADR-023)
- **Design Docs**: `docs/design/` (C4 diagrams, domain model, data flows)
- **Roadmap**: `plans/ROADMAP.md`
- **Engineering Guidelines**: `docs/engineering/`
