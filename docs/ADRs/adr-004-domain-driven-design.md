---
title: "ADR-004: Adopt Domain-Driven Design"
status: "Accepted"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["architecture", "domain", "design"]
supersedes: ""
superseded_by: ""
---

# ADR-004: Adopt Domain-Driven Design

## Status

### Accepted

## Context

ATLAS is a permit processing platform with complex business rules around application workflows, approval processes, and audit compliance. We need a design approach that:

1. **Captures domain complexity** - Permit processing has nuanced rules (application states, officer assignments, document requirements) that must be clearly modeled
2. **Aligns with business language** - Terms like "Permit Application", "Officer Review", "Audit Trail" must be consistent between business stakeholders and code
3. **Supports Clean Architecture** - DDD patterns naturally fit within the Clean Architecture layers (ADR-001)
4. **Enables bounded contexts** - Future extensions (payments, notifications, reporting) should be separable
5. **Facilitates testing** - Domain logic should be testable without infrastructure concerns

Alternative approaches considered:

- **Anemic Domain Model** - Simple data classes with logic in services (too coupled to infrastructure, harder to test)
- **Transaction Script** - Procedure-oriented business logic (doesn't scale with complexity, harder to maintain)
- **Table-Driven Design** - Database-first with minimal domain logic (violates Clean Architecture dependency rule)

## Decision

We will adopt **Domain-Driven Design (DDD)** principles as described by Eric Evans, implemented within the Clean Architecture framework (ADR-001).

### Core DDD Patterns to Apply

#### 1. **Entities** (Domain Layer)

Objects with identity continuity (tracked by ID across time):

- `Application` - Core entity with unique application ID
- `PermitType` - Configurable permit definitions
- `Document` - Uploaded file metadata with blob reference
- `Review` - Officer review comments linked to applications
- `User` - System users (Citizens, Officers, Administrators)
- `AuditLog` - Immutable record of system actions (entity with identity for 7-year retention)

#### 2. **Value Objects** (Domain Layer)

Immutable objects defined by their attributes (no identity):

- `ApplicationStatus` - Value object (Draft, Submitted, UnderReview, InfoRequested, Resubmitted, Approved, Rejected)
- `DocumentType` - Enumeration of accepted file types
- `AuditLog` - Immutable record of system actions

#### 3. **Aggregates** (Domain Layer)

Clusters of entities/value objects treated as a unit for data changes:

- **Application Aggregate Root** - Ensures application state transitions are valid. Contains `Document` and `Review` entities. `Application` IS the aggregate root — invariants are enforced directly by `Application` entity methods (`Submit`, `Approve`, `Reject`, `AddReview`, etc.) and the `internal` constructors on owned entities (`Document`, `Review`, `ApplicationFieldValue`).
- **PermitType Aggregate Root** - `PermitType` IS the aggregate root (no separate aggregate class needed). Contains `PermitField` and `DocumentRequirement` value objects.
- **User Aggregate Root** - `User` IS the aggregate root (no separate aggregate class needed). Simple entity with no child entities.

#### 4. **Domain Events** (Domain Layer)

Events that capture state changes for audit and extensibility:

- `ApplicationSubmittedEvent`
- `ApplicationApprovedEvent`
- `ApplicationRejectedEvent`
- `ApplicationInfoRequestedEvent`
- `ApplicationUnderReviewEvent`
- `ApplicationResubmittedEvent`
- `DocumentUploadedEvent`
- `PermitTypeDeactivatedEvent`
<!-- UserRoleChangedEvent removed in ADR-013: roles are Entra-driven -->

#### 5. **Repositories** (Application Layer Interfaces)

Abstractions for persistence operations:

- `IApplicationRepository`
- `IPermitTypeRepository`
- `IDocumentRepository`
- `IUserRepository`
- `IAuditLogRepository`

#### 6. **Domain Services** (Domain Layer)

Logic that doesn't belong to a single entity:

- `ApplicationEligibilityService` - Validates application completeness
- `AuditTrailService` - Ensures 7-year retention compliance (PRD F-20)

### Project Structure Alignment

```text
src/ATLAS.Domain/
├── Entities/
│   ├── Application.cs
│   ├── PermitType.cs
│   ├── Document.cs
│   ├── Review.cs
│   ├── User.cs
│   └── AuditLog.cs
├── Enums/
│   ├── ApplicationStatus.cs
│   ├── FieldType.cs
│   └── UserRole.cs
├── ValueObjects/
│   ├── DocumentType.cs
│   ├── PermitField.cs
│   └── DocumentRequirement.cs
├── Events/
│   ├── ApplicationSubmittedEvent.cs
│   ├── ApplicationApprovedEvent.cs
│   ├── ApplicationRejectedEvent.cs
│   ├── ApplicationInfoRequestedEvent.cs
│   ├── ApplicationUnderReviewEvent.cs
│   ├── ApplicationResubmittedEvent.cs
│   ├── DocumentUploadedEvent.cs
│   └── PermitTypeDeactivatedEvent.cs
├── Services/
│   ├── ApplicationEligibilityService.cs
│   └── AuditTrailService.cs
└── Interfaces/
    ├── IApplicationRepository.cs
    ├── IPermitTypeRepository.cs
    ├── IDocumentRepository.cs
    ├── IUserRepository.cs
    └── IAuditLogRepository.cs
```

## Consequences

### Positive

1. **Ubiquitous Language** - Business terms (Permit, Application, Review) are consistent in code, PRD, and discussions
2. **Encapsulation** - Business rules live in domain layer, not scattered across services or controllers
3. **Testability** - Domain logic (entity state transitions, validation rules) is unit-testable without databases
4. **Clean Architecture Alignment** - DDD layers map directly to Clean Architecture layers (ADR-001)
5. **Audit Compliance** - Domain events provide immutable audit trail (PRD F-20, 7-year retention)
6. **Future Extensibility** - Bounded contexts allow adding payments, notifications later without breaking core domain

### Negative

1. **Complexity** - More classes and patterns than simple CRUD (entities, value objects, aggregates, events)
2. **Learning Curve** - Team must understand DDD tactical patterns (entities vs value objects, aggregate boundaries)
3. **Over-Engineering Risk** - Simple entities might not need full DDD treatment (mitigated by pragmatic application)
4. **Performance Considerations** - Loading aggregates might require careful ORM configuration (EF Core)

### Mitigations

- **Pragmatic DDD** - Apply DDD where it adds value; simple CRUD entities can be simpler
- **Training** - Conduct workshop on DDD tactical patterns using ATLAS domain examples
- **Code Templates** - Provide starter templates for common DDD patterns
- **EF Core Configuration** - Use explicit loading and aggregation boundaries to avoid performance issues

## Alternatives Considered

### Anemic Domain Model

- **ALT-001**: **Description**: Data classes with properties only; business logic in service classes
- **ALT-002**: **Rejection Reason**: Violates encapsulation, scatters business rules across services, harder to test domain logic in isolation

### Transaction Script

- **ALT-003**: **Description**: Procedure-oriented approach with service methods for each use case
- **ALT-004**: **Rejection Reason**: Doesn't scale with domain complexity; permit workflow rules would become spaghetti code

### Table-Driven Design (Database-First)

- **ALT-005**: **Description**: Design database tables first, generate entities from schema
- **ALT-006**: **Rejection Reason**: Violates Clean Architecture dependency rule (domain depends on infrastructure); harder to evolve domain independently

## Implementation Notes

- **IMP-001**: Start with Application aggregate as the core bounded context
- **IMP-002**: Define aggregate boundaries carefully - avoid large aggregates that load too much data
- **IMP-003**: Use domain events for audit trail (PRD F-20) and future extensibility (notifications, reporting)
- **IMP-004**: Implement value objects as immutable types with validation in constructors
- **IMP-005**: Repository interfaces in Domain layer; implementations in Infrastructure layer (ADR-001 dependency rule)

## Compliance with Requirements

| Requirement | How DDD Addresses It |
| ----------- | --------------------- |
| PRD F-01: Submit permit application | Application entity encapsulates submission logic |
| PRD F-12: Approve application | Application.Approve() method enforces business rules |
| PRD F-20: Audit history | Domain events provide immutable audit trail |
| PRD C-01: .NET 9 | DDD patterns implemented using C# 12/.NET 9 features |
| ADR-001: Clean Architecture | DDD layers align with Clean Architecture layers |
| ADR-002: CQRS with MediatR | Domain events integrate with MediatR notifications |

## References

- **REF-001**: [ADR-001: Clean Architecture](adr-001-clean-architecture.md)
- **REF-002**: [ADR-002: CQRS with MediatR](adr-002-cqrs-mediatr.md)
- **REF-003**: [Domain Model Design](../../docs/design/03-domain-model.md)
- **REF-004**: [ATLAS PRD - Functional Requirements](../PRDs/atlas-mvp-prd.md#functional-requirements)
- **REF-005**: [Domain-Driven Design - Eric Evans](https://domainlanguage.com/ddd/)
- **REF-006**: [DDD Aggregate Pattern - Vaughn Vernon](https://www.dddcommunity.org/library/vernon_2011/)
