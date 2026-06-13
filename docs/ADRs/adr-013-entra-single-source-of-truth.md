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
