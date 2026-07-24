# Domain Model

## Overview

The ATLAS domain model represents the core business concepts for the permit processing platform. This model follows Domain-Driven Design (DDD) principles with clearly defined entities, value objects, aggregates, and their relationships.

## Core Domain Concepts

Based on the [ATLAS PRD](../PRDs/atlas-mvp-prd.md), the primary domain concepts are:

1. **Application** - A permit application submitted by a citizen
2. **PermitType** - Configurable permit categories defined by administrators
3. **User** - Synchronized Entra ID projection (Citizens, Officers, Administrators)
4. **Document** - Files uploaded in support of applications
5. **Review** - Officer's review of an application
6. **AuditLog** - Immutable record of all system actions

## Domain Model Diagram

```mermaid
classDiagram
    class Application {
        +Guid Id
        +string ApplicationNumber
        +ApplicationStatus Status
        +DateTime SubmittedDate
        +DateTime? ReviewedDate
        +string CitizenNotes
        +string OfficerNotes
        +Guid CitizenId
        +Guid PermitTypeId
        +Submit()
        +Approve(Guid officerId, string reason)
        +Reject(Guid officerId, string reasonCode, string comments)
        +RequestInfo(Guid officerId, string message)
    }

    class PermitType {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +List~PermitField~ Fields
        +List~DocumentRequirement~ DocumentRequirements
        +decimal Fee
        +CreateField(name, type, required)
        +AddDocumentRequirement(docType, required)
        +Activate()
        +Deactivate()
    }

    class User {
        +Guid Id
        +string Email
        +string FirstName
        +string LastName
        +UserRole Role
        +DateTime? LastLoginDate
        +RecordLogin()
        +GetFullName()
        +SynchronizeFromClaims()
    }

    class Document {
        +Guid Id
        +Guid ApplicationId
        +string FileName
        +string ContentType
        +long FileSize
        +string BlobUrl
        +DateTime UploadedDate
        +Guid UploadedById
    }

    class Review {
        +Guid Id
        +Guid ApplicationId
        +Guid OfficerId
        +ReviewDecision Decision
        +string ReasonCode
        +string Comments
        +DateTime ReviewedDate
        +bool IsVisibleToCitizen
    }

    class AuditLog {
        +Guid Id
        +Guid? UserId
        +string Action
        +string EntityType
        +Guid EntityId
        +string Details
        +DateTime Timestamp
        +string IpAddress
    }

    class PermitField {
        +string Name
        +FieldType Type
        +bool IsRequired
        +string DefaultValue
    }

    class DocumentRequirement {
        +string DocumentType
        +bool IsRequired
        +string[] AllowedExtensions
        +long MaxFileSizeBytes
    }

    Application "1" --> "1" PermitType : has type
    Application "1" --> "1" User : submitted by (Citizen)
    Application "1" --> "*" Document : contains
    Application "1" --> "*" Review : has reviews
    Review "1" --> "1" User : reviewed by (Officer)
    Document "1" --> "1" User : uploaded by
    PermitType "1" --> "*" PermitField : defines
    PermitType "1" --> "*" DocumentRequirement : requires
    AuditLog "0..1" --> "1" User : performed by
```

## Relationships

| From | To | Relationship | Description |
| ------ | ---- | -------------- | ------------- |
| Application | PermitType | Many-to-One | Each application is for a specific permit type |
| Application | User (Citizen) | Many-to-One | Each application is submitted by one citizen |
| Application | Document | One-to-Many | An application can have multiple supporting documents |
| Application | Review | One-to-Many | An application can have multiple reviews (history) |
| Review | User (Officer) | Many-to-One | Each review is conducted by one officer |
| Document | User | Many-to-One | Each document is uploaded by one user |
| PermitType | PermitField | One-to-Many | Permit types define multiple fields |
| PermitType | DocumentRequirement | One-to-Many | Permit types specify document requirements |
| AuditLog | User | Many-to-One (optional) | Audit entries may be associated with a user |

## Value Objects

### ApplicationStatus (Enum)

```text
Submitted → UnderReview → Approved
                     ↘ InfoRequested → Resubmitted → UnderReview
                     ↘ Rejected
```

### UserRole (Enum)

- `Citizen` - Can submit and track applications
- `Officer` - Can review and process applications
- `Admin` - Can manage system configuration

### ReviewDecision (Enum)

- `Approve` - Application approved
- `Reject` - Application rejected with reason code
- `RequestInfo` - Additional information requested

### FieldType (Enum)

- `Text` - Free-text input
- `MultilineText` - Multi-line text area
- `Number` - Numeric input
- `Date` - Date picker
- `Boolean` - Boolean checkbox
- `Dropdown` - Single select from options

## Domain Invariants

1. **Application must have a valid PermitType** - Cannot submit without selecting type
2. **Application status transitions must be valid** - Cannot go from Approved to Submitted
3. **Rejection requires a reason code** - Mandatory field per PRD F-13
4. **Documents must be associated with an application** - Orphaned documents not allowed
5. **AuditLog entries are immutable** - Once created, cannot be modified or deleted
6. **PermitType must be active** - Cannot submit application for inactive permit type

## Domain Events

| Event | Trigger | Payload |
| ------- | --------- | --------- |
| `ApplicationSubmitted` | Citizen submits application | ApplicationId, CitizenId, PermitTypeId, Timestamp |
| `ApplicationApproved` | Officer approves application | ApplicationId, OfficerId, Timestamp |
| `ApplicationRejected` | Officer rejects application | ApplicationId, OfficerId, ReasonCode, Comments |
| `ApplicationAssignedToOfficerEvent` | Officer assigned to application | ApplicationId, OfficerId, OccurredOn |
| `ApplicationUnderReviewEvent` | Application status changes to UnderReview | ApplicationId, Timestamp |
| `DocumentUploaded` | User uploads document | DocumentId, ApplicationId, UserId, FileName |
| `PermitTypeCreated` | Admin creates permit type | PermitTypeId, AdminId, Name |
| `AuditLogCreated` | Any system action | Action, UserId, EntityType, EntityId |

## References

- [ATLAS PRD - Functional Requirements](../PRDs/atlas-mvp-prd.md#5-functional-requirements)
- [ATLAS PRD - User Stories](../PRDs/atlas-mvp-prd.md#4-specifications--use-cases)
- [Domain-Driven Design](https://domainlanguage.com/ddd/)
