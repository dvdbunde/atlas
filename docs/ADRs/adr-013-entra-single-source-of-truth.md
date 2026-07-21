---
title: "ADR-013: Entra ID as Single Source of Truth for User Identity"
status: "Accepted"
date: "2026-06-13"
authors: "Engineering Team"
tags: ["security", "authentication", "entra-id", "identity-management", "architecture"]
supersedes: ""
superseded_by: ""
---

# ADR-013: Entra ID as Single Source of Truth for User Identity

## Status

### Accepted

## Context

ATLAS initially implemented user lifecycle management within the application itself. This included commands to create, update, and deactivate users, change roles, and manage identity profiles. After completing Milestone 4 (authentication/authorization via Entra ID), the architecture had a fundamental contradiction:

- **Entra ID** was the authentication provider and role authority
- **ATLAS Domain** still owned user lifecycle behavior (mutation methods, commands, API endpoints)

This created a dual-write risk where user data could drift between Entra ID and ATLAS.

## Decision

Microsoft Entra ID is the **sole source of truth** for user identity lifecycle, profile data, and role assignment. The ATLAS User aggregate is a **synchronized local projection**.

### Ownership Boundaries

| Domain | Owner | ATLAS Behavior |
| -------- | ------- | ---------------- |
| User creation | Entra ID | Automatic, idempotent synchronization |
| User profile | Entra ID | Synchronized from claims |
| User roles | Entra ID | Read from claim values |
| User activation | Entra ID | Reflected in sync |
| Business relationships | ATLAS | Ownership, assignments, reporting |
| Audit trail | ATLAS | Immutable record of business operations |

### Synchronization Architecture

`ext
Entra ID (single source of truth)
    │ JWT Bearer token (oid, given_name, family_name, email, roles)
    ▼
ICurrentUserService → UserSynchronizationBehavior → IdentityResolver
    │
    ▼
User.SynchronizeFromClaims(email, firstName, lastName, role)  [idempotent]
    │
    ▼
Local User record (read-only synchronized projection)
`

### Removed Functionality

- User.ChangeRole(), User.Deactivate(), User.UpdateEmail(), User.UpdateProfile()
- IsActive property, UserRoleChangedEvent
- CreateUserCommand, UpdateUserRoleCommand
- POST /api/users, PUT /api/users/{id}/role

## Consequences

1. ✅ Eliminated dual-write risk
2. ✅ Simplified domain model
3. ✅ Automatic Entra-to-ATLAS propagation
4. ⚠ Synchronization latency (one request cycle)
5. ⚠ No offline user management through ATLAS

## Appendix: Future Architecture Backlog

**B-013-1 — Rename `IUserRepository.UpdateAsync` to a synchronization-oriented API (e.g. `SynchronizeAsync`).**

- **Rationale:** `IUserRepository` inherits the generic `IRepository<T>.UpdateAsync`, which reads as a general-purpose CRUD update and could invite a future handler to locally mutate `User` identity data — reintroducing the dual-write anti-pattern ADR-013 prohibits. A synchronization-specific name makes the projection intent structural rather than merely documented.
- **Current state:** `UpdateAsync(User)` is invoked **only** from `IdentityResolver.SynchronizeUserAsync` (the Entra→ATLAS sync path). It is not called by any command handler or UI. `DeleteAsync` was removed from `UserRepository` entirely (ATLAS never deletes user projections).
- **Why deferred:** Renaming requires changing the generic `IRepository<T>` contract or overriding the method name on `IUserRepository` + `UserRepository` + the `IdentityResolver` call site. The generic `UpdateAsync` is also used by other repositories (`PermitType`, `Application`, `Document`) where it is legitimate. Impact was assessed as excessive for the alignment cleanup, so it is tracked here as a backlog item rather than implemented now.
- **Guardrail:** Until renamed, the ADR-013 architecture tests (`Adr013UserProjectionGuardTests`) assert that `IUserRepository.UpdateAsync` is referenced **only** from `IdentityResolver`, preventing misuse.
