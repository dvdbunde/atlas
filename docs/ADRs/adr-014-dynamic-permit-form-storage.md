---
title: "ADR-014: Dynamic Permit Form Storage Strategy"
status: "Accepted"
date: "2026-06-18"
authors: "Engineering Team"
tags: ["architecture", "domain", "persistence", "dynamic-forms", "milestone-5"]
supersedes: ""
superseded_by: ""
---

# ADR-014: Dynamic Permit Form Storage Strategy

## Status

### Accepted

## Context

Milestone 5 introduced the Permit Submission workflow, which requires dynamic form generation based on permit type configuration. Each permit type (e.g., Building Permit, Business License) defines a configurable set of fields. When a citizen fills out an application, the values for those fields must be stored and associated with the application.

The key challenge was choosing a storage strategy for application field values that balances:

- **Schema flexibility**: Permit types can have different fields, and field definitions can change over time
- **Data integrity**: Field values must remain associated with the correct application and permit type
- **Queryability**: Field values must be readable for display, editing, and review
- **Aggregate consistency**: Field values are part of the Application aggregate and must respect aggregate boundaries

The existing architecture had the following relevant elements:

- **`PermitField`** (Value Object): Defines field configuration (name, type, required, default value) for a permit type
- **`Application`** (Aggregate Root): Owns `Document` and `Review` entities via EF Core `OwnsMany`
- **`ApplicationAggregate`**: Enforces invariants for application state transitions
- **`PermitType`** (Entity): Defines permit categories with configurable fields and requirements

## Decision

We introduce an **`ApplicationFieldValue`** entity as an owned child of the `Application` aggregate, persisted via EF Core `OwnsMany` configuration. Field values reference permit fields by **`FieldName`** (a string) rather than by a foreign key to `PermitField`.

### Storage Model

\`\`\`
Application (Aggregate Root)
├── Documents (OwnsMany)
├── Reviews (OwnsMany)
└── FieldValues (OwnsMany)
    ├── FieldName: string (references PermitField.Name)
    ├── Value: string (normalized storage)
    ├── SortOrder: int
    ├── CreatedDate: DateTime
    └── ModifiedDate: DateTime?
\`\`\`

### Key Design Decisions

1. **`ApplicationFieldValue` as Entity**: Although field values are conceptually value-like, they are implemented as entities with an `Id` to support EF Core `OwnsMany` tracking, individual updates (`UpdateFieldValue`), and removal (`RemoveFieldValue`). Each instance has identity within the aggregate.

2. **`FieldName` reference strategy**: Field values reference their corresponding permit field definition by `FieldName` (string), not by `PermitFieldId` (foreign key). This decouples the Application aggregate from the PermitType aggregate and avoids cross-aggregate references.

3. **Normalized storage (row per field)**: Each field value is stored as a separate row in an `ApplicationFieldValue` table, rather than as a JSON blob. This allows individual field updates without rewriting the entire set, supports querying individual fields, and maintains a clean relational model.

4. **No separate repository**: Field values are accessed and persisted through the `Application` aggregate only. There is no `IApplicationFieldValueRepository`. This enforces aggregate boundary rules — all changes to field values go through the `Application` aggregate root methods (`AddFieldValue`, `UpdateFieldValue`, `RemoveFieldValue`).

5. **No `DocumentId` and no `PermitFieldId`**: The `ApplicationFieldValue` entity does not include a `DocumentId` field (document association is handled separately through the `Application.Documents` collection) or a `PermitFieldId` field (field definition reference is by `FieldName`).

6. **String-based value storage**: All field values are stored as strings, regardless of their `FieldType` (Text, Number, Date, Boolean, Dropdown, MultilineText). Type-specific validation and formatting occur at the application layer, not in persistence.

## Consequences

### Positive

- **POS-001**: **Aggregate integrity preserved** — Field values are within the Application aggregate boundary, ensuring consistency through the aggregate root
- **POS-002**: **No cross-aggregate references** — `FieldName` strategy avoids coupling between Application and PermitType aggregates
- **POS-003**: **Individual field updates** — Normalized storage allows updating single fields without rewriting all values
- **POS-004**: **Consistent with existing patterns** — Follows the same `OwnsMany` pattern already used for `Documents` and `Reviews`
- **POS-005**: **Simple schema** — Single table with `ApplicationId`, `FieldName`, `Value`, and `SortOrder`; no complex joins
- **POS-006**: **No JSON parsing overhead** — Normalized rows avoid serialization/deserialization costs of JSON storage

### Negative

- **NEG-001**: **No foreign key constraint to PermitField** — `FieldName` is a string without referential integrity enforcement at the database level; referential correctness depends on application-layer validation
- **NEG-002**: **String storage for all types** — All values are stored as strings, requiring type conversion at read time for display and filtering
- **NEG-003**: **Schema migration for new field types** — Adding new `FieldType` values requires application-layer handling but no database migration
- **NEG-004**: **No type-specific indexing** — Since all values are strings, database-level filtering by value type (e.g., date range queries) is not straightforward

## Alternatives Considered

### Alternative 1: JSON Blob Storage

Store all field values as a single JSON string in a column on the `Applications` table.

- **ALT-001**: **Description**: A single `FieldValuesJson` column (nvarchar(max)) stores all field values as a serialized JSON object
- **ALT-002**: **Rejection Reason**: JSON storage would require reading and writing the entire blob for any individual field change, increasing data transfer and conflict risk. JSON parsing is required on every read. Querying individual field values requires JSON functions (OPENJSON, JSON_VALUE), adding complexity. This approach also deviates from the existing `OwnsMany` pattern used for `Documents` and `Reviews`.

### Alternative 2: Separate Repository for FieldValues

Create a dedicated `IApplicationFieldValueRepository` and persist `ApplicationFieldValue` as an independent entity with its own DbSet.

- **ALT-003**: **Description**: `ApplicationFieldValue` would have its own repository, DbSet, and could be queried independently of the `Application` aggregate
- **ALT-004**: **Rejection Reason**: This violates DDD aggregate boundary rules. Field values have no meaning outside the context of an Application. A separate repository would allow bypassing aggregate invariants (e.g., adding field values to a submitted application) and create consistency challenges. The Application aggregate root must own and control access to all child entities.

### Alternative 3: PermitFieldId Foreign Key Approach

Add a `PermitFieldId` foreign key column to `ApplicationFieldValue` that directly references `PermitField.Id`.

- **ALT-005**: **Description**: Instead of `FieldName`, use a GUID foreign key (`PermitFieldId`) to link field values to their permit field definition
- **ALT-006**: **Rejection Reason**: This creates a cross-aggregate reference between the `Application` aggregate and the `PermitType` aggregate, which is a DDD anti-pattern. If a permit type field is deleted or renamed, all existing application field values would have dangling references or require cascade updates. The `FieldName` strategy provides a stable, human-readable reference that survives permit type configuration changes.

## Implementation Notes

- **IMP-001**: `ApplicationFieldValue` is in the `ATLAS.Domain.Entities` namespace alongside `Application`, `Document`, and `Review`
- **IMP-002**: EF Core configuration uses `builder.OwnsMany(a => a.FieldValues, field => { ... })` in `ApplicationConfiguration.cs`
- **IMP-003**: Field values are loaded eagerly with the Application aggregate via EF Core Include/ThenInclude or lazy loading
- **IMP-004**: The `Application.AddFieldValue()` method enforces uniqueness of `FieldName` within an application (case-insensitive)
- **IMP-005**: Required field validation occurs at the application layer before submission, not in the domain entity
- **IMP-006**: Database migration `M5_ApplicationFieldValues` creates the `ApplicationFieldValue` table with cascade delete to the parent application

## References

- **REF-001**: [ADR-004: Domain-Driven Design](adr-004-domain-driven-design.md) — Aggregate boundaries and ownership rules
- **REF-002**: [ATLAS Domain Model Design](../../docs/design/03-domain-model.md) — PermitField value object and Application aggregate
- **REF-003**: [PRD F-01, F-02, F-07](../../docs/PRDs/atlas-mvp-prd.md) — Dynamic form and draft application requirements
- **REF-004**: [ApplicationConfiguration.cs](../../src/ATLAS.Infrastructure/Data/Configurations/ApplicationConfiguration.cs) — EF Core OwnsMany configuration
- **REF-005**: [ApplicationFieldValue.cs](../../src/ATLAS.Domain/Entities/ApplicationFieldValue.cs) — Entity implementation
- **REF-006**: [Application.cs](../../src/ATLAS.Domain/Entities/Application.cs) — Aggregate root with FieldValues collection
