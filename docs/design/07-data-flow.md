# Data Flow Diagrams

## Overview

This document illustrates the key data flows and process interactions in ATLAS using sequence diagrams. These flows show how data moves between the Blazor Web App, ASP.NET Core API, Domain Layer, and external systems.

## Prerequisites

- **User is authenticated** via Microsoft Entra ID (all user types — Citizen, Officer, Admin)
- **User has appropriate role** (Citizen, Officer, Admin) for the operation

---

## Flow 1: Citizen Submits Permit Application

```mermaid
sequenceDiagram
    actor Citizen
    participant Blazor as Blazor Web App
    participant API as ASP.NET Core API
    participant MediatR as MediatR
    participant Handler as Command Handler
    participant Domain as Domain Layer
    participant Repo as Repository
    participant SQL as Azure SQL
    participant Blob as Azure Blob Storage
    participant Email as Email Service

    Citizen->>Blazor: Navigate to New Application
    Blazor->>API: GET /api/permittypes (active only)
    API->>Repo: GetAllActiveAsync()
    Repo->>SQL: SELECT * FROM PermitTypes WHERE IsActive=1
    SQL-->>Repo: List<PermitType>
    Repo-->>API: List<PermitType>
    API-->>Blazor: JSON PermitTypes
    Blazor-->>Citizen: Display permit type dropdown

    Citizen->>Blazor: Select permit type & fill form
    Blazor-->>Citizen: Render dynamic fields (PermitType.Fields)

    Citizen->>Blazor: Upload documents (PDF/JPG/PNG)
    Blazor->>Blob: PUT /documents/{appId}/{fileName}
    Blob-->>Blazor: 201 Created + BlobUrl
    Blazor-->>Blazor: Store BlobUrl in form state

    Citizen->>Blazor: Click "Submit Application"
    Blazor->>API: POST /api/applications {permitTypeId, formData, documents}
    API->>MediatR: Send SubmitApplicationCommand
    MediatR->>Handler: Handle(command)

    Handler->>Repo: GetByIdAsync(permitTypeId)
    Repo->>SQL: SELECT * FROM PermitTypes WHERE Id=@Id
    SQL-->>Repo: PermitType
    Repo-->>Handler: PermitType

    Handler->>Domain: new Application(citizenId, permitTypeId, formData)
    Domain->>Domain: Validate business rules
    Domain->>Domain: application.Submit()
    Domain-->>Handler: Application (Status=Submitted)

    Handler->>Repo: AddAsync(application)
    Repo->>SQL: INSERT INTO Applications VALUES(...)
    SQL-->>Repo: Success
    Repo-->>Handler: ApplicationId

    Handler->>MediatR: Publish ApplicationSubmittedEvent
    MediatR->>Email: Send confirmation email
    Email-->>Citizen: "Application Submitted - Ref: PERMIT-20260602-0001"

    Handler-->>API: Return ApplicationId
    API-->>Blazor: 201 Created + ApplicationId
    Blazor-->>Citizen: Display confirmation with application number
```

**Key Data Transformations:**

- Form data → `Application` entity (Domain Layer)
- Uploaded files → Azure Blob Storage (BlobUrl stored in `Document` entity)
- `SubmitApplicationCommand` → `ApplicationSubmittedEvent` (MediatR)

---

## Flow 2: Officer Reviews Application

```mermaid
sequenceDiagram
    actor Officer
    participant Blazor as Blazor Web App
    participant API as ASP.NET Core API
    participant MediatR as MediatR
    participant Handler as Command Handler
    participant Domain as Domain Layer
    participant Repo as Repository
    participant SQL as Azure SQL
    participant Blob as Azure Blob Storage
    participant Email as Email Service

    Officer->>Blazor: Navigate to Dashboard
    Blazor->>API: GET /api/applications?status=UnderReview&department=Building
    API->>Repo: GetByStatusAndDepartment(status, department)
    Repo->>SQL: SELECT * FROM Applications WHERE Status=@Status AND Department=@Dept
    SQL-->>Repo: List<Application>
    Repo-->>API: List<Application>
    API-->>Blazor: JSON Applications (summary)
    Blazor-->>Officer: Display application queue

    Officer->>Blazor: Click on application to review
    Blazor->>API: GET /api/applications/{id}
    API->>Repo: GetByIdAsync(id)
    Repo->>SQL: SELECT * FROM Applications WHERE Id=@Id
    SQL-->>Repo: Application (with Documents, Reviews)
    Repo-->>API: Application
    API-->>Blazor: JSON Application (full details)

    Blazor->>Blob: GET /documents/{docId}?sasToken
    Blob-->>Blazor: Document stream (PDF/JPG/PNG)
    Blazor-->>Officer: Display application details + documents

    Officer->>Blazor: Add internal notes (not visible to citizen)
    Blazor->>API: PATCH /api/applications/{id}/notes {notes}
    API->>Repo: UpdateAsync(application)
    Repo->>SQL: UPDATE Applications SET OfficerNotes=@Notes
    SQL-->>Repo: Success
    Repo-->>API: Success
    API-->>Blazor: 200 OK
    Blazor-->>Officer: Notes saved confirmation

    Officer->>Blazor: Click "Approve" (or "Reject" with reason)
    Blazor->>API: POST /api/applications/{id}/approve {comments}

    API->>MediatR: Send ApproveApplicationCommand
    MediatR->>Handler: Handle(command)
    Handler->>Repo: GetByIdAsync(id)
    Repo->>SQL: SELECT * FROM Applications WHERE Id=@Id
    SQL-->>Repo: Application
    Repo-->>Handler: Application

    Handler->>Domain: application.Approve(officerId, comments)
    Domain->>Domain: Validate status = UnderReview
    Domain->>Domain: Update status to Approved
    Domain-->>Handler: Updated Application

    Handler->>Repo: UpdateAsync(application)
    Repo->>SQL: UPDATE Applications SET Status=@Status, ReviewedDate=@Date
    SQL-->>Repo: Success

    Handler->>MediatR: Publish ApplicationApprovedEvent
    MediatR->>Email: Send approval notification
    Email-->>Citizen: "Application Approved - Ref: PERMIT-20260602-0001"

    Handler-->>API: Success
    API-->>Blazor: 200 OK
    Blazor-->>Officer: Display success message, refresh dashboard
```

**Key Data Transformations:**

- `ApproveApplicationCommand` → `application.Approve()` → `ApplicationApprovedEvent`
- Officer notes stored in `Application.OfficerNotes` (not visible to citizens)
- Status change triggers email notification via MediatR domain event

---

## Flow 3: Administrator Manages Permit Types

```mermaid
sequenceDiagram
    actor Admin
    participant Blazor as Blazor Web App
    participant API as ASP.NET Core API
    participant MediatR as MediatR
    participant Handler as Command Handler
    participant Domain as Domain Layer
    participant Repo as Repository
    participant SQL as Azure SQL
    participant Audit as AuditLog Service

    Admin->>Blazor: Navigate to Permit Type Management
    Blazor->>API: GET /api/permittypes (all, including inactive)
    API->>Repo: GetAllAsync()
    Repo->>SQL: SELECT * FROM PermitTypes
    SQL-->>Repo: List<PermitType>
    Repo-->>API: List<PermitType>
    API-->>Blazor: JSON PermitTypes (full details)
    Blazor-->>Admin: Display permit type list with actions

    Admin->>Blazor: Click "Create New Permit Type"
    Blazor-->>Admin: Display create form (name, description, fee)

    Admin->>Blazor: Configure fields (name, type, required)
    Blazor-->>Blazor: Dynamic field builder UI

    Admin->>Blazor: Configure document requirements
    Blazor-->>Blazor: Document requirement builder UI

    Admin->>Blazor: Click "Create Permit Type"
    Blazor->>API: POST /api/permittypes {name, description, fee, fields, documents}
    API->>MediatR: Send CreatePermitTypeCommand
    MediatR->>Handler: Handle(command)

    Handler->>Domain: new PermitType(name, description, fee)
    Domain->>Domain: permitType.AddField(...) for each field
    Domain->>Domain: permitType.AddDocumentRequirement(...) for each doc
    Domain-->>Handler: PermitType

    Handler->>Repo: AddAsync(permitType)
    Repo->>SQL: INSERT INTO PermitTypes VALUES(...)
    SQL-->>Repo: Success
    Repo-->>Handler: PermitTypeId

    Handler->>MediatR: Publish PermitTypeCreatedEvent
    MediatR->>Audit: Create AuditLog entry
    Audit->>SQL: INSERT INTO AuditLogs VALUES(...)

    Handler-->>API: Return PermitTypeId
    API-->>Blazor: 201 Created + PermitTypeId
    Blazor-->>Admin: Display success, redirect to permit type list

    Admin->>Blazor: Click "Deactivate" on permit type
    Blazor->>API: PATCH /api/permittypes/{id}/deactivate
    API->>MediatR: Send DeactivatePermitTypeCommand
    MediatR->>Handler: Handle(command)
    Handler->>Repo: GetByIdAsync(id)
    Repo->>SQL: SELECT * FROM PermitTypes WHERE Id=@Id
    SQL-->>Repo: PermitType
    Handler->>Domain: permitType.Deactivate()
    Handler->>Repo: UpdateAsync(permitType)
    Repo->>SQL: UPDATE PermitTypes SET IsActive=0
    Handler->>MediatR: Publish PermitTypeDeactivatedEvent
    Handler-->>API: Success
    API-->>Blazor: 200 OK
    Blazor-->>Admin: Permit type deactivated confirmation
```

**Key Data Transformations:**

- Form data → `PermitType` entity with `PermitField` and `DocumentRequirement` value objects
- `CreatePermitTypeCommand` → `PermitTypeCreatedEvent` → `AuditLog` entry
- Deactivation sets `IsActive=false` (soft delete, existing applications unaffected)

---

## Data Flow Summary

| Flow | Trigger | Key Data Stores | Domain Events |
| ------ | ---------- | ----------------- | --------------- |
| **Citizen Submission** | Submit Application button | Azure SQL (Application), Blob Storage (Documents) | `ApplicationSubmittedEvent` |
| **Officer Review** | Approve/Reject button | Azure SQL (Application, Review), Blob Storage (read) | `ApplicationApprovedEvent` / `ApplicationRejectedEvent` |
| **Admin Permit Type** | Create/Update/Deactivate | Azure SQL (PermitType) | `PermitTypeCreatedEvent`, `PermitTypeDeactivatedEvent` |

## References

- [ATLAS PRD - Use Cases](../PRDs/atlas-mvp-prd.md#4-specifications--use-cases)
- [Container Diagram](02-container-diagram.md)
- [Domain Model](03-domain-model.md)
- [CQRS Pattern with MediatR](https://github.com/jbogard/MediatR)
