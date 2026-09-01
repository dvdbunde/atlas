# Architecture Decision Records

ADRs document significant architectural decisions along with their context and consequences. They create a durable record that helps future maintainers understand why choices were made.

## ADR Approval Process

**Mission Critical**: All ADRs must be created in a branch and submitted for review, before being merged to the main branch.

## Index

Add links to ADRs in sequential order using the naming convention `adr-NNNN-title-slug.md`.

- [ADR-0001: Clean Architecture](adr-001-clean-architecture.md) ✅ Implemented
- [ADR-0002: CQRS with MediatR](adr-002-cqrs-mediatr.md) ✅ Implemented
- [ADR-0003: Azure SQL & Blob Storage](adr-003-azure-sql-blob.md) ✅ Implemented (LocalDB dev, Azurite local storage)
- [ADR-0004: Domain-Driven Design](adr-004-domain-driven-design.md) ✅ Implemented
- [ADR-0005: Blazor Server Web App](adr-005-blazor-web-app.md) ✅ Implemented
- [ADR-0006: GitHub Actions](adr-006-github-actions.md) ✅ Implemented (CI only; CD deferred)
- [ADR-0007: Bicep Infrastructure as Code](adr-007-bicep.md) 🟡 Accepted, not implemented (deferred to Milestone 9)
- [ADR-0008: Microsoft Entra ID](adr-008-microsoft-entra-id.md) ✅ Implemented
- [ADR-0009: Azure Key Vault](adr-009-azure-key-vault.md) 🟡 Partially Implemented (packages installed, full integration deferred to M9)
- [ADR-0010: Row-Level Security](adr-010-row-level-security.md) 🟡 Accepted (app-layer filtering implemented; RLS deferred)
- [ADR-0011: Data Lifecycle Management](adr-011-data-lifecycle-management.md) 🟡 Proposed (not yet implemented)
- [ADR-0012: Generated API Layer with NSwag](adr-012-generated-api-layer.md) ✅ Implemented
- [ADR-0013: Entra ID as Single Source of Truth for User Identity](adr-013-entra-single-source-of-truth.md) ✅ Implemented
- [ADR-0014: Dynamic Permit Form Storage Strategy](adr-014-dynamic-permit-form-storage.md) ✅ Implemented
- [ADR-0015: Document Storage Architecture](adr-015-document-storage-architecture.md) ✅ Implemented
- [ADR-0016: Transaction Pipeline via MediatR TransactionBehavior](adr-016-transaction-behavior.md) ✅ Implemented
- [ADR-0017: Citizen Editing After Information Request](adr-017-citizen-editing-after-information-request.md) ✅ Implemented
- [ADR-0018: Shared Application Details Layout with RenderFragment Composition](adr-018-shared-application-details-layout.md) ✅ Implemented
- [ADR-0019: Azure App Service as the Primary Production Hosting Platform](adr-019-azure-app-service-primary-hosting.md) ✅ Implemented
- [ADR-0020: Infrastructure as Code Using Bicep](adr-020-infrastructure-as-code-bicep.md) ✅ Implemented
- [ADR-0021: Environment-Based Configuration and Secret Management](adr-021-environment-based-configuration-secrets.md) ✅ Implemented
- [ADR-0022: Managed Azure Services over Self-Hosted Infrastructure](adr-022-managed-azure-services-preference.md) ✅ Implemented
- [ADR-0023: Production Observability Strategy](adr-023-production-observability-strategy.md) ✅ Implemented
- [ADR-0024: Grafana Cloud for Operational Visualization](adr-024-grafana-cloud-operational-visualization.md) ✅ Accepted

## ADR Template

Use [adr-template.md](adr-template.md) when creating new ADRs.
