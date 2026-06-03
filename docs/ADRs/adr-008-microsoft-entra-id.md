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

ATLAS requires authentication and authorization for three user types: Citizens (submitters), Permit Officers (reviewers), and Administrators (configuration). The PRD specifies (Constraint C-05): "Microsoft Entra ID for government employees + local accounts for citizens (MVP)".

Key security and compliance requirements:

1. **Role-based access control (RBAC)** - Different permissions for Citizens, Officers, Admins (PRD F-21)
2. **Government employee authentication** - Officers and Admins are municipal employees with Entra ID accounts
3. **Citizen access** - Citizens may not have Entra ID, need local accounts for MVP (future: Entra External ID)
4. **Audit compliance** - All authentication events must be logged (PRD F-20, 7-year retention)
5. **Multi-factor authentication (MFA)** - Government employees require MFA (security best practice)
6. **Integration with Azure** - App Service and SQL Database support Entra ID authentication (ADR-003, ADR-007)

Alternative authentication approaches considered:

- **ASP.NET Core Identity with local accounts only** - All users local, no Entra ID integration (violates C-05)
- **Auth0 or Okta** - Third-party identity providers, additional cost, separate from Azure ecosystem
- **Windows Authentication** - On-premises only, doesn't work for cloud-hosted App Service
- **Custom JWT tokens** - Build custom auth system (high security risk, not compliant with government standards)

## Decision

We will use **Microsoft Entra ID** (formerly Azure Active Directory) for authenticating government employees (Officers, Admins) and **ASP.NET Core Identity with local accounts** for citizens in the MVP.

### Authentication Architecture

```text
┌────────────────────────────────────────────────────┐
│                   Users                            │
│                                                    │
│  ┌──────────────┐    ┌──────────────────────┐      │
│  │   Citizens   │    │  Gov Employees       │      │
│  │  (No Entra)  │    │  (Entra ID accounts) │      │
│  └──────┬───────┘    └───────────┬──────────┘      │
│         │                        │                 │
└─────────┼────────────────────────┼─────────────────┘
          │                        │
          │ Local accounts         │ OAuth 2.0 + OIDC
          │ (MVP)                  │ + MFA
          │                        │
┌─────────▼────────────────────────▼─────────────────┐
│           ATLAS Application (Blazor Server)        │
│                                                    │
│  ┌──────────────────────────────────────────────┐  │
│  │  Authentication Middleware                   │  │
│  │                                              │  │
│  │  ┌────────────────────────────────────────┐  │  │
│  │  │  ASP.NET Core Identity (Citizens)      │  │  │
│  │  │  - Local username/password             │  │  │
│  │  │  - Stored in Azure SQL (ADR-003)       │  │  │
│  │  └────────────────────────────────────────┘  │  │
│  │                                              │  │
│  │  ┌────────────────────────────────────────┐  │  │
│  │  │  Microsoft Entra ID (Officers/Admins)  │  │  │
│  │  │  - OAuth 2.0 + OpenID Connect          │  │  │
│  │  │  - MFA enforced                        │  │  │
│  │  │  - Integrated with App Service         │  │  │
│  │  └────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────┘  │
│                                                    │
│  ┌──────────────────────────────────────────────┐  │
│  │  Role-Based Authorization (RBAC)             │  │
│  │  - Citizen: Submit/view own applications     │  │
│  │  - Officer: Review/approve applications      │  │
│  │  - Admin: Manage permit types, audit logs    │  │
│  └──────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────┘
```

### User Types and Authentication Methods

| User Type | Authentication Method | MFA Required | Account Storage |
| ---------- | --------------------- | ------------- | --------------- |
| **Citizens** | ASP.NET Core Identity (local accounts) | No (MVP) | Azure SQL Database |
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
2. **Single sign-on (SSO)** - Officers/Admins use existing Entra ID credentials (no new passwords)
3. **Secure by default** - MFA enforced for government employees (NIST compliance)
4. **Scalable authorization** - RBAC with roles maps to PRD requirements (F-21)
5. **Azure integration** - App Service and SQL Database support Entra ID authentication (ADR-003, ADR-007)
6. **Audit trail** - Entra ID sign-in logs complement application audit (PRD F-20)
7. **Future-ready** - Citizens can migrate to Entra External ID post-MVP without architecture changes

### Negative

1. **Dual authentication systems** - Local accounts (Citizens) + Entra ID (Employees) increases complexity
2. **Citizen experience** - Citizens must create local accounts (no SSO with government portals in MVP)
3. **Entra ID licensing** - Requires Entra ID P1 or P2 for MFA (additional cost, but typical for government)
4. **Configuration complexity** - App Service must be configured for both authentication schemes
5. **Local account security** - Must implement password policies, account lockout (ASP.NET Core Identity)

### Mitigations

- **Unified auth middleware** - Use `Microsoft.AspNetCore.Authentication` to handle both schemes seamlessly
- **Citizen account guidelines** - Document password requirements and account recovery process
- **Entra ID P1 license** - Justify cost as mandatory for government compliance (MFA requirement)
- **Security best practices** - Follow OWASP guidelines for local account management (password hashing, rate limiting)
- **Post-MVP roadmap** - Plan migration to Entra External ID for citizens (unified authentication)

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
- **IMP-002**: Enable MFA for Entra ID users (Officers, Admins) via Conditional Access policies
- **IMP-003**: Configure ASP.NET Core Identity with password policies (min length, complexity, lockout)
- **IMP-004**: Store local account credentials in Azure SQL (separate from Entra ID, per ADR-003)
- **IMP-005**: Use `AuthorizeAttribute` and `Roles` property for RBAC in Blazor and API controllers
- **IMP-006**: Log all authentication events to audit trail (PRD F-20) via domain events (ADR-004)
- **IMP-007**: Configure App Service Authentication (Easy Auth) to accept both Entra ID and local accounts

## Compliance with Requirements

| Requirement | How Entra ID Addresses It |
| ----------- | ------------------------- |
| PRD C-05: Entra ID for employees | ✅ Officers/Admins authenticate via Entra ID with MFA |
| PRD C-05: Local accounts for citizens | ✅ Citizens use ASP.NET Core Identity (MVP approach) |
| PRD F-21: Manage user accounts/roles | ✅ RBAC with Citizen/Officer/Admin roles |
| PRD F-20: Audit history | ✅ Entra ID sign-in logs + application audit trail |
| PRD NFR-06: Security (MFA) | ✅ MFA enforced for government employees via Entra ID |
| ADR-001: Clean Architecture | ✅ Authentication in Presentation layer, user context passed to Domain |
| ADR-003: Azure SQL | ✅ Local account credentials stored in Azure SQL |
| ADR-007: Bicep | ✅ Entra ID App Registration deployed via Bicep |

## References

- **REF-001**: [ADR-001: Clean Architecture](adr-001-clean-architecture.md)
- **REF-002**: [ADR-003: Azure SQL + Blob Storage](adr-003-azure-sql-blob.md)
- **REF-003**: [ADR-004: Domain-Driven Design](adr-004-domain-driven-design.md)
- **REF-004**: [ADR-007: Bicep Infrastructure as Code](adr-007-bicep.md)
- **REF-005**: [ATLAS PRD - Security Requirements](../PRDs/atlas-mvp-prd.md#non-functional-requirements)
- **REF-006**: [Microsoft Entra ID Documentation](https://learn.microsoft.com/en-us/entra/identity/)
- **REF-007**: [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- **REF-008**: [Entra ID and Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)
