---
title: "ADR-021: Environment-Based Configuration and Secret Management"
status: "Accepted"
date: "2026-07-28"
authors: "Engineering Team"
tags: ["architecture", "configuration", "secrets", "security", "azure", "milestone-9"]
supersedes: ""
superseded_by: ""
---

# ADR-021: Environment-Based Configuration and Secret Management

## Status

### Accepted

## Context

ATLAS operates in multiple environments: local development, CI/CD pipelines, and production Azure. Each environment requires different configuration values — database connection strings, storage account keys, API endpoints, and feature flags. Some of these values are sensitive and must be protected from unauthorized access.

Earlier milestones established the need for secure secrets management (ADR-009) and introduced Azure Key Vault references. However, the overall configuration architecture — how configuration values are sourced, layered, and resolved at runtime — was not formally defined. Without a clear architecture, developers risk inconsistent patterns across environments, accidental exposure of production secrets, and configuration drift between environments.

The ASP.NET Core configuration provider model provides a layered approach where configuration sources are overridden in order of precedence. This model is well-suited to the environment hierarchy ATLAS requires, but the project must commit to a consistent pattern rather than mixing approaches.

## Decision

ATLAS follows the standard ASP.NET Core configuration provider model, with environment-specific sources layered according to the following precedence (highest priority last):

### Development

1. **appsettings.json**: Non-sensitive defaults and shared configuration.
2. **appsettings.Development.json**: Development-specific overrides (local database names, local blob storage endpoints).
3. **User Secrets**: Developer-specific secrets (local connection strings, local Entra ID client secrets). Never committed to source control.

### Production

1. **appsettings.json**: Non-sensitive defaults and shared configuration.
2. **appsettings.Production.json**: Production-specific overrides (Azure resource endpoints, production URLs).
3. **Azure App Service Application Settings**: Environment variables injected by App Service configuration. Managed via Bicep, not the portal.
4. **Azure Key Vault**: Secrets referenced by Key Vault references in App Service configuration. Accessed via Managed Identity — no secrets in configuration files.
5. **Managed Identity**: Used for authentication to Azure SQL Database and Azure Blob Storage. No connection strings or access keys are stored or transmitted.

### Constraint

No production secrets are stored in source control. This includes connection strings, access keys, client secrets, API keys, and certificates. Any value that is both sensitive and environment-specific must be sourced from Azure Key Vault or managed identity at runtime.

## Consequences

### Positive

- **Secure by default**: Production secrets never enter the repository. Key Vault access is authenticated via managed identity, which requires no stored credentials.
- **Environment isolation**: Each environment loads the correct configuration values without hard-coding environment-specific logic. A misconfigured environment is immediately visible because the wrong values produce incorrect behavior.
- **Developer productivity**: User Secrets provide a local development experience that mirrors production configuration without requiring Azure connectivity. Developers can run the full application stack locally.
- **Audit trail**: Key Vault access logs provide an audit trail of secret retrieval. Changes to App Service application settings are tracked through Bicep history.
- **Consistent pattern**: Every environment uses the same configuration provider chain. The order of precedence is predictable and documented.

### Negative

- **Configuration sprawl**: A value can potentially be defined in multiple sources. Tracing which source supplies a given value requires understanding the precedence order.
- **Key Vault latency**: Application startup may be delayed by Key Vault cache initialization. This is mitigated by the Key Vault configuration provider's built-in caching and retry logic.
- **Local development gap**: Some Azure services (Key Vault, managed identity) are not available in the local development environment. Local equivalents must be configured via User Secrets, which may not perfectly replicate production behavior.

## Alternatives Considered

### appsettings Only

- **Description**: All configuration, including secrets, stored in `appsettings` files with environment-specific variants.
- **Rejection Reason**: Production secrets would be present in source control, violating security policy. No access control, no audit trail, no rotation capability.

### Environment Variables Only

- **Description**: All configuration provided through system environment variables. No `appsettings` files.
- **Rejection Reason**: Loses the structured, hierarchical nature of ASP.NET Core configuration. Environment variables are flat strings with no support for sections, arrays, or typed binding. Makes local development cumbersome because every variable must be set before running.

### Azure App Configuration

- **Description**: A centralized Azure service for managing application configuration, with feature management and dynamic refresh capabilities.
- **Rejection Reason**: Adds operational complexity (another Azure service to manage, configure, and authenticate) without immediate benefit. The standard ASP.NET Core provider model, combined with App Service Application Settings and Key Vault, covers ATLAS's current configuration needs. Azure App Configuration may be evaluated in a future milestone if dynamic configuration or feature flags become a requirement.

## References

- ADR-008: Microsoft Entra ID (managed identity authentication for Key Vault access)
- ADR-009: Azure Key Vault (Key Vault secret storage and access patterns)
- ADR-019: Azure App Service (App Service Application Settings and managed identity integration)
- ADR-020: Infrastructure as Code Using Bicep (App Service configuration deployed via Bicep)
