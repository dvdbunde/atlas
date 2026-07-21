# Core Entities

## Overview

This document defines the core entities in the ATLAS domain model. Each entity follows Domain-Driven Design (DDD) principles with a unique identity, encapsulated state, and behavior that enforces business rules.

## Entity Definitions

### 1. Application Entity

The central entity representing a permit application in the system.

| Property | Type | Description | Business Rules |
| ---------- | ------ | ------------- | ---------------- |
| `Id` | Guid | Unique identifier | Generated on creation, immutable |
| `ApplicationNumber` | string | Human-readable reference number | Format: `PERMIT-{YYYY}{MM}{DD}-{####}` |
| `Status` | ApplicationStatus | Current state of the application | See state machine below |
| `SubmittedDate` | DateTime | When the application was submitted | Set on Submit(), null before |
| `ReviewedDate` | DateTime? | When the application was last reviewed | Updated on Approve/Reject |
| `CitizenNotes` | string | Notes from the citizen during submission | Max 2000 characters |
| `OfficerNotes` | string | Internal notes from officers | Not visible to citizens, max 5000 chars |
| `CitizenId` | Guid | Reference to the submitting citizen | Must be a valid User with Citizen role |
| `PermitTypeId` | Guid | Reference to the permit type applied for | Must be an active PermitType |

**Methods:**

```csharp
public class Application : Entity<Guid>
{
    public void Submit()
    {
        if (Status != ApplicationStatus.Draft)
            throw new DomainException("Only draft applications can be submitted");

        Status = ApplicationStatus.Submitted;
        SubmittedDate = DateTime.UtcNow;
        AddDomainEvent(new ApplicationSubmittedEvent(Id, CitizenId, PermitTypeId));
    }

    public void Approve(Guid officerId, string comments)
    {
        if (Status != ApplicationStatus.UnderReview)
            throw new DomainException("Only applications under review can be approved");

        Status = ApplicationStatus.Approved;
        ReviewedDate = DateTime.UtcNow;
        OfficerNotes += $"\n[APPROVED {DateTime.UtcNow} by {officerId}]: {comments}";
        AddDomainEvent(new ApplicationApprovedEvent(Id, officerId));
    }

    public void Reject(Guid officerId, string reasonCode, string comments)
    {
        if (Status != ApplicationStatus.UnderReview)
            throw new DomainException("Only applications under review can be rejected");

        if (string.IsNullOrWhiteSpace(reasonCode))
            throw new DomainException("Rejection requires a reason code");

        Status = ApplicationStatus.Rejected;
        ReviewedDate = DateTime.UtcNow;
        OfficerNotes += $"\n[REJECTED {DateTime.UtcNow} by {officerId}]: Reason: {reasonCode}. {comments}";
        AddDomainEvent(new ApplicationRejectedEvent(Id, officerId, reasonCode));
    }
}
```

**Status State Machine:**

```text
[Draft] → Submit() → [Submitted] → Assign to Officer → [UnderReview]
                                                   ↓
                            Approve() → [Approved]
                            Reject() → [Rejected]
                            RequestInfo() → [InfoRequested] → Citizen updates → [Resubmitted] → [UnderReview]
```

---

### 2. User Entity (Entra Synchronized Projection)

Represents a synchronized local projection of an Entra ID principal (Citizen, Officer, Administrator).
The User entity is NOT an independently-managed identity account — it is a business entity
used for ownership, assignments, auditing, and reporting. All identity data is sourced from
Entra ID claims via the synchronization pipeline.

| Property | Type | Description | Source |
| -------- | ---- | ----------- | ------ |
| `Id` | Guid | Entra ID object ID (oid claim) | Entra ID |
| `Email` | string | User's email address | Entra ID `email` claim |
| `FirstName` | string | User's first name | Entra ID `given_name` claim |
| `LastName` | string | User's last name | Entra ID `family_name` claim |
| `Role` | UserRole | System role (Citizen/Officer/Admin) | Entra ID `roles` claim |
| `LastLoginDate` | DateTime? | Last successful authentication | Updated on each sync |

**Synchronization Method:**

```csharp
public void SynchronizeFromClaims(string email, string firstName, string lastName, UserRole role)
{
    // Only updates values that have actually changed (idempotent)
    // Does NOT raise domain events — this is a passive sync operation
}
```

**Business Methods:**

```csharp
public void RecordLogin()
{
    LastLoginDate = DateTime.UtcNow;
}

public string GetFullName()
{
    return $"{FirstName} {LastName}";
}
```

**Removed Methods (Entra-first architecture, see ADR-013):**

- `ChangeRole()` — role mutation removed; roles come from Entra
- `Deactivate()` — lifecycle removed; activation managed in Entra
- `UpdateEmail()` — profile mutation removed; email comes from Entra
- `UpdateProfile()` — profile mutation removed; name comes from Entra
- `IsActive` property — lifecycle removed; activation managed in Entra

> **Archived historical example (ADR-013 Appendix — DO NOT USE):** The code below is preserved only as historical context showing what ADR-013 removed. It is **not** the current model and must not be copied into new code. In the current architecture, role mutations and deactivation are managed exclusively by Microsoft Entra ID; the `User` entity no longer has `ChangeRole()`, `Deactivate()`, `UpdateEmail()`, `UpdateProfile()`, `IsActive`, or the `UserRoleChangedEvent`/`UserDeactivatedEvent` events. See [ADR-013](../ADRs/adr-013-entra-single-source-of-truth.md) for the authoritative decision.

```csharp
// ARCHIVED — removed by ADR-013. Kept for historical reference only.
public void ChangeRole(UserRole newRole, Guid changedByAdminId)
{
    if (Role == newRole)
        return;

    var oldRole = Role;
    Role = newRole;
    AddDomainEvent(new UserRoleChangedEvent(Id, oldRole, newRole, changedByAdminId));
}

public void Deactivate(Guid deactivatedByAdminId)
{
    if (!IsActive)
        return;

    IsActive = false;
    AddDomainEvent(new UserDeactivatedEvent(Id, deactivatedByAdminId));
}
```

---

### 3. PermitType Entity

Configurable permit categories defined by administrators.

| Property | Type | Description | Business Rules |
| ---------- | ------ | ------------- | ---------------- |
| `Id` | Guid | Unique identifier | Generated on creation |
| `Name` | string | Permit type name | Required, unique, 3-100 chars |
| `Description` | string | Detailed description | Max 2000 characters |
| `IsActive` | bool | Whether citizens can apply | Inactive hides from citizens |
| `Fields` | List\<PermitField\> | Configurable form fields | Defined by administrators |
| `DocumentRequirements` | List\<DocumentRequirement\> | Required documents | Defined by administrators |
| `Fee` | decimal | Permit fee amount | Must be >= 0 |

**Methods:**

```csharp
public void AddField(string name, FieldType type, bool isRequired, string defaultValue = null)
{
    if (Fields.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        throw new DomainException($"Field '{name}' already exists");

    Fields.Add(new PermitField(name, type, isRequired, defaultValue));
    AddDomainEvent(new PermitTypeFieldAddedEvent(Id, name, type));
}

public void AddDocumentRequirement(string documentType, bool isRequired, string[] allowedExtensions, long maxFileSize)
{
    if (DocumentRequirements.Any(d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase)))
        throw new DomainException($"Document requirement '{documentType}' already exists");

    DocumentRequirements.Add(new DocumentRequirement(documentType, isRequired, allowedExtensions, maxFileSize));
}

public void Deactivate(Guid deactivatedByAdminId)
{
    if (!IsActive)
        return;

    IsActive = false;
    AddDomainEvent(new PermitTypeDeactivatedEvent(Id, deactivatedByAdminId));
}
```

---

### 4. Document Entity

Represents files uploaded in support of permit applications.

| Property | Type | Description | Business Rules |
| ---------- | ------ | ------------- | ---------------- |
| `Id` | Guid | Unique identifier | Generated on upload |
| `ApplicationId` | Guid | Parent application | Must be valid Application |
| `FileName` | string | Original file name | Required, max 255 chars |
| `ContentType` | string | MIME type | Must be PDF, JPG, or PNG |
| `FileSize` | long | File size in bytes | Max 25MB per PRD F-03 |
| `BlobUrl` | string | Azure Blob Storage URL | Secure access URL |
| `UploadedDate` | DateTime | When file was uploaded | Set on creation |
| `UploadedById` | Guid | User who uploaded | Citizen or Officer |

---

### 5. Review Entity

Represents an officer's review of an application.

| Property | Type | Description | Business Rules |
| ---------- | ------ | ------------- | ---------------- |
| `Id` | Guid | Unique identifier | Generated on review |
| `ApplicationId` | Guid | Reviewed application | Must be valid Application |
| `OfficerId` | Guid | Reviewing officer | Must be User with Officer role |
| `Decision` | ReviewDecision | Approve/Reject/RequestInfo | Required |
| `ReasonCode` | string | Rejection reason (if rejected) | Required if Decision=Reject |
| `Comments` | string | Officer's comments | Max 5000 characters |
| `ReviewedDate` | DateTime | When review occurred | Set on creation |
| `IsVisibleToCitizen` | bool | Whether citizen can see | Internal notes = false |

---

### 6. AuditLog Entity

Immutable record of all system actions for compliance (7-year retention per PRD).

| Property | Type | Description | Business Rules |
| ---------- | ------ | ------------- | ---------------- |
| `Id` | Guid | Unique identifier | Generated on action |
| `UserId` | Guid? | User who performed action | Null for system actions |
| `Action` | string | Action performed | e.g., "APPLICATION_SUBMITTED" |
| `EntityType` | string | Type of entity affected | e.g., "Application" |
| `EntityId` | Guid | ID of affected entity | |
| `Details` | string | Additional context | JSON or human-readable |
| `Timestamp` | DateTime | When action occurred | UTC timestamp |
| `IpAddress` | string | User's IP address | For audit trail |

**Note:** AuditLog entities are **immutable** - no update or delete operations allowed.

## Entity Relationships Summary

```text
User (Citizen) "1" ── "0..*" Application
User (Officer) "1" ── "0..*" Review
Application "1" ── "1" PermitType
Application "1" ── "0..*" Document
Application "1" ── "0..*" Review
PermitType "1" ── "0..*" PermitField (Value Object)
PermitType "1" ── "0..*" DocumentRequirement (Value Object)
```

## References

- [ATLAS PRD - Functional Requirements](../PRDs/atlas-mvp-prd.md#5-functional-requirements)
- [Domain Model](03-domain-model.md)
- [Aggregate Roots](05-aggregate-roots.md)
