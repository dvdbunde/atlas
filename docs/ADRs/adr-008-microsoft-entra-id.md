---
title: "ADR-008: Use Microsoft Entra ID for Authentication"
status: "Accepted"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["security", "authentication", "entra-id", "azure-ad"]
supersedes: ""
superseded_by: ""
---

# ADR-008: Use Microsoft Entra ID for Authentication

## Status

### Accepted

## Context

ATLAS requires authentication and authorization for three user types: Citizens (submitters), Permit Officers (reviewers), and Administrators (configuration).

Key security and compliance requirements:

1. **Role-based access control (RBAC)** - Different permissions for Citizens, Officers, Admins (PRD F-21)
2. **Government employee authentication** - Officers and Admins are municipal employees with Entra ID accounts
3. **Citizen access** - Citizens authenticate through Microsoft Entra ID (Entra External ID)
4. **Audit compliance** - All authentication events must be logged (PRD F-20, 7-year retention)
5. **Multi-factor authentication (MFA)** - Government employees require MFA (security best practice)
6. **Integration with Azure** - App Service and SQL Database support Entra ID authentication (ADR-003, ADR-007)

Alternative authentication approaches considered:

- **ASP.NET Core Identity with local accounts only** - All users local, no Entra ID integration (violates C-05)
- **Auth0 or Okta** - Third-party identity providers, additional cost, separate from Azure ecosystem
- **Windows Authentication** - On-premises only, doesn't work for cloud-hosted App Service
- **Custom JWT tokens** - Build custom auth system (high security risk, not compliant with government standards)

## Decision

All users — Citizens, Officers, and Administrators — authenticate through **Microsoft Entra ID** (formerly Azure Active Directory). No local accounts, passwords, or separate authentication stores are used.

### Authentication Architecture

```text
┌──────────────────────────────────────────────┐
│                 Users                        │
│                                              │
│  ┌──────────────┐  ┌──────────┐  ┌─────────┐ │
│  │   Citizens   │  │ Officers │  │  Admins │ │
│  └──────┬───────┘  └────┬─────┘  └────┬────┘ │
│         │               │             │      │
└─────────┼───────────────┼─────────────┼──────┘
          │               │             │
          └───────────────┼─────────────┘
                          │
               OAuth 2.0 + OIDC + MFA
                          │
          ┌───────────────▼──────────────────────┐
          │      Microsoft Entra ID              │
          │  (single identity provider)          │
          └───────────────┬──────────────────────┘
                          │
                          │ JWT Bearer token
                          │
┌─────────────────────────▼──────────────────────────┐
│              ATLAS API (ATLAS.API)                 │
│                                                    │
│  ┌───────────────────────────────────────────────┐ │
│  │  Authentication Pipeline                      │ │
│  │                                               │ │
│  │  JWT Bearer Authentication (Entra ID)         │ │
│  │         ↓                                     │ │
│  │  AtlasClaimsTransformation                    │ │
│  │    (Entra ID roles → ClaimTypes.Role)         │ │
│  │         ↓                                     │ │
│  │  Authorization Policies                       │ │
│  │    (Citizen, Officer, Admin, OfficerOrAdmin)  │ │
│  │         ↓                                     │ │
│  │  GeneratedControllerAuthorizationConvention   │ │
│  │    (convention-based policies on generated    │ │
│  │     controllers, survives NSwag regeneration) │ │
│  └───────────────────────────────────────────────┘ │
│                                                    │
│  ┌───────────────────────────────────────────────┐ │
│  │  ICurrentUserService → IIdentityResolver      │ │
│  │  Resolves JWT claims to ATLAS.Domain.User     │ │
│  │  Auto-provisions + syncs on every request     │ │
│  └───────────────────────────────────────────────┘ │
│                                                    │
│  ┌───────────────────────────────────────────────┐ │
│  │  Role-Based Authorization (RBAC)              │ │
│  │  - Citizen: Submit/view own applications      │ │
│  │  - Officer: Review/approve applications       │ │
│  │  - Admin: Manage permit types, audit logs     │ │
│  └───────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────┘
```

### User Types and Authentication Methods

| User Type | Authentication Method | MFA Required | Account Storage |
| ---------- | --------------------- | ------------- | --------------- |
| **Citizens** | Microsoft Entra ID (External ID) | Recommended | Entra ID tenant |
| **Permit Officers** | Microsoft Entra ID | Yes | Entra ID tenant |
| **Administrators** | Microsoft Entra ID | Yes | Entra ID tenant |

### Role Definitions

```csharp
public static class Roles
{
    public const string Citizen = "Citizen";
    public const string Officer = "Officer";
    public const string Admin = "Admin";
}

// Authorization in Blazor components
<AuthorizeView Roles="Officer,Admin">
    <Authorized>
        <p>You can review applications.</p>
    </Authorized>
    <NotAuthorized>
        <p>You do not have permission.</p>
    </NotAuthorized>
</AuthorizeView>
```

## Consequences

### Positive

1. **Government standards compliance** - Entra ID meets government security requirements (MFA, audit logs)
2. **Single sign-on (SSO)** - All users use existing Entra ID credentials (no new passwords for anyone)
3. **Secure by default** - MFA for all user types (NIST compliance)
4. **Scalable authorization** - RBAC with roles maps to PRD requirements (F-21)
5. **Azure integration** - App Service and SQL Database support Entra ID authentication (ADR-003, ADR-007)
6. **Audit trail** - Entra ID sign-in logs complement application audit (PRD F-20)
7. **Simplified architecture** - Single identity provider eliminates dual-auth complexity
8. **No local credential storage** - No passwords to hash, no lockout policies, no password reset flows
9. **Future-ready** - Citizens already on Entra ID; no migration needed for Entra External ID

### Negative

1. **Entra ID licensing** - Requires Entra ID P1 or P2 for MFA and Conditional Access (additional cost, but typical for government)
2. **Citizen onboarding** - Citizens must have or create a Microsoft account or social identity for Entra External ID
3. **Configuration complexity** - Entra ID tenant must be configured with proper app registrations, redirect URIs, and role mappings

### Mitigations

- **Entra ID P1 license** - Justify cost as mandatory for government compliance (MFA requirement for all users)
- **Citizen onboarding documentation** - Create guide for citizens to set up Entra External ID accounts
- **IT collaboration** - Work with IT department early to configure Entra ID tenant, app registrations, and role assignments
- **Post-MVP roadmap** - Evaluate Entra External ID migration path if citizen onboarding friction is identified

## Alternatives Considered

### ASP.NET Core Identity Only (Local Accounts for All)

- **ALT-001**: **Description**: All users (Citizens, Officers, Admins) use local accounts
- **ALT-002**: **Rejection Reason**: Violates PRD Constraint C-05 (requires Entra ID for government employees), no MFA for officers/admins, not compliant with government security standards

### Auth0 or Okta (Third-Party Identity Providers)

- **ALT-003**: **Description**: Cloud identity platforms with support for multiple auth schemes
- **ALT-004**: **Rejection Reason**: Additional cost, separate from Azure ecosystem, Entra ID already available in Azure subscription, no need for third-party

### Windows Authentication (On-Premises)

- **ALT-005**: **Description**: Integrated Windows Authentication for intranet users
- **ALT-006**: **Rejection Reason**: Doesn't work for cloud-hosted App Service, citizens cannot use (external users), not compatible with modern web apps

### Custom JWT Token Authentication

- **ALT-007**: **Description**: Build custom authentication with JWT token issuance and validation
- **ALT-008**: **Rejection Reason**: High security risk (custom auth is hard to secure), not compliant with government standards, violates "don't roll your own crypto" principle

## Implementation Notes

- **IMP-001**: Configure Entra ID tenant with App Registration for ATLAS (redirect URIs, API permissions)
- **IMP-002**: Enable MFA for all users via Conditional Access policies
- **IMP-005**: Use `AuthorizeAttribute` and `Roles` property for RBAC in Blazor and API controllers
- **IMP-006**: Log all authentication events to audit trail (PRD F-20) via domain events (ADR-004)
- **IMP-008**: Role claims (`Citizen`, `Officer`, `Admin`) are assigned via Entra ID app roles or group membership — not stored locally
- **IMP-009**: Domain User auto-provisioning via `IIdentityResolver.SynchronizeUserAsync()` runs in the MediatR pipeline on every authenticated request
- **IMP-010**: No ASP.NET Core Identity, no local usernames/passwords, no email verification, no password reset, no account lockout — all delegated to Entra ID
- **IMP-011**: The `POST /api/users` endpoint has been **removed** — user creation is now automatic and idempotent via `IIdentityResolver.SynchronizeUserAsync()` on first authenticated request. See [ADR-013](adr-013-entra-single-source-of-truth.md) for the full architecture.
- **IMP-012**: Swagger UI uses OAuth2 Authorization Code flow against Entra ID. The OpenAPI `EntraID` security scheme uses `type: openIdConnect` with the Entra ID OIDC metadata URL. The Swagger security definition is configured as `SecuritySchemeType.OAuth2` with Authorization Code flow, reading `AzureAd:TenantId` and `AzureAd:ClientId` from configuration at startup. The OAuth scope is `api://{ClientId}/access_as_user`. `OAuthUsePkce()` is enabled for production-grade token acquisition.

## Compliance with Requirements

| Requirement | How Entra ID Addresses It |
| ----------- | ------------------------- |
| PRD C-05: Authentication for all users | ✅ All users (Citizens, Officers, Admins) authenticate via Microsoft Entra ID |
| PRD F-21: Manage user accounts/roles | ✅ RBAC with Citizen/Officer/Admin roles assigned via Entra ID app roles |
| PRD F-20: Audit history | ✅ Entra ID sign-in logs + application audit trail |
| PRD NFR-06: Security (MFA) | ✅ MFA for all users via Entra ID Conditional Access |
| ADR-001: Clean Architecture | ✅ Authentication in Presentation layer, user context passed to Domain via `ICurrentUserService` |
| ADR-003: Azure SQL | ✅ No local credentials stored — authentication delegated entirely to Entra ID |
| ADR-007: Bicep | ✅ Entra ID App Registration deployed via Bicep |

## References

- **REF-001**: [ADR-001: Clean Architecture](adr-001-clean-architecture.md)
- **REF-002**: [ADR-003: Azure SQL + Blob Storage](adr-003-azure-sql-blob.md)
- **REF-003**: [ADR-004: Domain-Driven Design](adr-004-domain-driven-design.md)
- **REF-004**: [ADR-007: Bicep Infrastructure as Code](adr-007-bicep.md)
- **REF-005**: [ATLAS PRD - Security Requirements](../PRDs/atlas-mvp-prd.md#non-functional-requirements)
- **REF-006**: [Microsoft Entra ID Documentation](https://learn.microsoft.com/en-us/entra/identity/)
- **REF-007**: [Entra ID and Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)
- **REF-008**: [Entra External ID Documentation](https://learn.microsoft.com/en-us/entra/external-id/)
