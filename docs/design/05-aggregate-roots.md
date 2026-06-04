# Aggregate Roots

## Overview

Aggregate Roots are central entities in Domain-Driven Design that enforce invariants and control access to a cluster of related objects. This document defines the aggregate roots for ATLAS and their boundaries.

## Aggregate Definition

An **Aggregate** is a cluster of associated objects treated as a unit for data changes. The **Aggregate Root** is the entry point for all operations on the aggregate. External objects can only hold references to the aggregate root, not its internal entities.

## ATLAS Aggregate Roots

Based on the domain model, ATLAS has three aggregate roots. Note: `PermitType` and `User` ARE the aggregate roots themselves - they don't need separate aggregate classes. Only `Application` requires an aggregate class to enforce complex invariants.

---

### 1. Application Aggregate

**Root Entity:** `Application`

**Aggregate Class:** `ApplicationAggregate` (enforces invariants for Application and its child entities)

**Aggregate Members:**

- `Document` (entities) - Supporting files for the application
- `Review` (entities) - Officer reviews and decisions
- `ApplicationStatus` (value object) - Current state

**Aggregate Boundary:**

```text
Application (Root)
├── Document [0..*]
│   ├── Id
│   ├── FileName
│   ├── ContentType
│   └── BlobUrl
├── Review [0..*]
│   ├── Id
│   ├── OfficerId
│   ├── Decision
│   └── Comments
└── Status (ApplicationStatus)
```

**Invariants Enforced by Application Root:**

1. **Status transitions must be valid** - Cannot skip states or go backwards (except InfoRequested → Resubmitted)
2. **Rejection requires a reason code** - `ReasonCode` is mandatory when `Decision` = Reject
3. **Documents must belong to the application** - Cannot have orphaned documents
4. **Only one active review at a time** - New review creates history entry
5. **Application must have a valid PermitType** - Cannot submit without type selection
6. **Submitted date set on first submission** - Cannot be changed after submission

**Access Patterns:**

- External code can ONLY reference `Application` by its `Id`
- `Document` and `Review` objects are accessed through the `Application` root
- Example: `application.AddDocument(fileName, contentType, blobUrl)` NOT `new Document(...)`

**CQRS Command Handlers:**

```csharp
public class SubmitApplicationCommandHandler : IRequestHandler<SubmitApplicationCommand, Guid>
{
    private readonly IRepository<Application> _applicationRepository;

    public async Task<Guid> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId);
        application.Submit();  // Enforces invariants
        await _applicationRepository.UpdateAsync(application);
        return application.Id;
    }
}
```

---

### 2. PermitType Aggregate Root

**Root Entity:** `PermitType` (IS the aggregate root - no separate aggregate class needed)

**Aggregate Members:**

- `PermitField` (value objects) - Configurable form fields
- `DocumentRequirement` (value objects) - Document requirements

**Aggregate Boundary:**

```text
PermitType (Root)
├── PermitField [0..*]
│   ├── Name
│   ├── Type (FieldType)
│   ├── IsRequired
│   └── DefaultValue
└── DocumentRequirement [0..*]
    ├── DocumentType
    ├── IsRequired
    ├── AllowedExtensions
    └── MaxFileSizeBytes
```

**Invariants Enforced by PermitType Root:**

1. **Field names must be unique** - Cannot have duplicate field names (enforced in `AddField()`)
2. **Document types must be unique** - Cannot have duplicate document requirements (enforced in `AddDocumentRequirement()`)
3. **Cannot deactivate if active applications exist** - Business rule (checked in command handler)
4. **Fee must be non-negative** - `Fee >= 0` (enforced in constructor)
5. **Active permit types are visible to citizens** - `IsActive` controls visibility

**Access Patterns:**

- `PermitType` IS the aggregate root - no separate aggregate class
- `PermitField` and `DocumentRequirement` are value objects accessed through `PermitType`
- Modifications go through root methods: `permitType.AddField(...)`, `permitType.AddDocumentRequirement(...)`
- No direct manipulation of internal collections from outside

---

### 3. User Aggregate Root

**Root Entity:** `User` (IS the aggregate root - no separate aggregate class needed)

**Aggregate Members:**

- None (simple entity with no child entities)

**Aggregate Boundary:**

```text
User (Root)
├── Id
├── Email
├── FirstName
├── LastName
├── Role (UserRole)
├── IsActive
└── CreatedDate
```

**Invariants Enforced by User Root:**

1. **Email must be unique** - Enforced by database constraint, checked in command handler
2. **Role changes are audited** - Domain event raised on role change
3. **Inactive users cannot login** - Checked during authentication
4. **Email format must be valid** - Validated on creation/update

**Access Patterns:**

- `User` IS the aggregate root - no separate aggregate class needed
- `User` is a simple aggregate with no child entities
- Direct property access is acceptable (no internal entities to protect)
- Role changes go through `user.ChangeRole(newRole)` to enforce invariants

---

## Aggregate Design Decisions

### Why These Aggregates?

| Aggregate | Rationale |
| ----------- | ------------ |
| **Application** | Documents and Reviews only exist in context of an Application. They cannot be shared across applications. All status transitions must be atomic. Requires `ApplicationAggregate` class to enforce complex invariants. |
| **PermitType** | Fields and DocumentRequirements define the structure of a permit type. They are configuration data that only makes sense within the PermitType context. `PermitType` IS the aggregate root - no separate aggregate class needed. |
| **User** | Simple entity with no child objects. Could be split into separate aggregates for Citizen, Officer, Admin but kept unified for MVP simplicity. `User` IS the aggregate root - no separate aggregate class needed. |

### Aggregates NOT Used

| Concept | Why Not an Aggregate? |
| ---------- | ---------------------- |
| **AuditLog** | Read-only, immutable records. No business logic or invariants to enforce. Treated as a separate read model. |
| **Review** | Belongs to Application aggregate - a review cannot exist without an application. |
| **Document** | Belongs to Application aggregate - documents are always tied to a specific application. |

## Repository Pattern

Each aggregate root has a corresponding repository interface in the Domain layer:

```csharp
// Domain/Repositories/IApplicationRepository.cs
public interface IApplicationRepository
{
    Task<Application> GetByIdAsync(Guid id);
    Task AddAsync(Application application);
    Task UpdateAsync(Application application);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Application>> GetByCitizenIdAsync(Guid citizenId);
    Task<IEnumerable<Application>> GetByStatusAsync(ApplicationStatus status);
}

// Domain/Repositories/IPermitTypeRepository.cs
public interface IPermitTypeRepository
{
    Task<PermitType> GetByIdAsync(Guid id);
    Task<IEnumerable<PermitType>> GetAllActiveAsync();
    Task AddAsync(PermitType permitType);
    Task UpdateAsync(PermitType permitType);
}
```

## References

- [ATLAS Domain Model](03-domain-model.md)
- [Core Entities](04-core-entities.md)
- [Bounded Contexts](06-bounded-contexts.md)
- [Domain-Driven Design - Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
