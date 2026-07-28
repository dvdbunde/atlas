---
title: "ADR-020: Infrastructure as Code Using Bicep"
status: "Accepted"
date: "2026-07-28"
authors: "Engineering Team"
tags: ["architecture", "infrastructure", "bicep", "iac", "azure", "milestone-9"]
supersedes: ""
superseded_by: ""
---

# ADR-020: Infrastructure as Code Using Bicep

## Status

### Accepted

## Context

Milestone 9 provisions production Azure infrastructure. The deployment must be repeatable, auditable, and consistent across environments (development, staging, production). Earlier milestones established the choice of Bicep as the Infrastructure as Code tool (ADR-007), but the principle that _all_ infrastructure is provisioned through code rather than manual configuration was not yet formalized as an architectural constraint.

Without a mandatory IaC principle, individual team members might configure Azure resources through the portal for convenience, creating a gap between the deployed infrastructure and the codebase. This gap undermines reproducibility, complicates disaster recovery, and prevents peer review of infrastructure changes. The Milestone 9 deployment scope — multiple Azure resources spanning App Service, SQL Database, Blob Storage, Key Vault, and Application Insights — makes IaC enforcement essential.

## Decision

All Azure infrastructure shall be provisioned using declarative Bicep templates. Manual Azure Portal configuration should be limited to tenant-specific bootstrap activities only — for example, initial Entra ID app registration or tenant-level policy assignments — that cannot be expressed in Bicep. Every resource that supports Bicep deployment must be defined in the codebase.

### Principles

- **Reproducibility**: The same Bicep templates, parameterized per environment, produce identical infrastructure. A fresh deployment to a new environment follows the same codified process.
- **Version control**: Infrastructure templates are stored in the repository alongside application code. Changes are tracked in Git, reviewed through pull requests, and deployed through the same CI/CD pipeline (ADR-006).
- **Peer review**: Infrastructure changes follow the same review process as application code changes. A pull request that modifies a Bicep template is reviewed for correctness, security, and cost implications before merge.
- **Disaster recovery**: The infrastructure definition is recoverable from the repository. If a resource group is accidentally deleted, the Bicep templates, combined with the state of persistent data (database backups, blob storage), can recreate the environment.
- **Environment consistency**: Parameter files (one per environment) capture the differences between environments. The templates themselves are identical across environments, ensuring that staging and production are structurally equivalent.

## Consequences

### Positive

- **Single source of truth**: The repository is the authoritative definition of all infrastructure. There is no undocumented or manually configured infrastructure.
- **Audit trail**: Every infrastructure change is recorded in Git with a commit message, author, and pull request discussion. This supports compliance and post-incident analysis.
- **Reusable modules**: Common patterns (App Service + SQL Database, Key Vault access policies) are encapsulated in Bicep modules and reused across environments, reducing duplication and error.
- **Pre-deployment validation**: Bicep's `what-if` operation shows the exact changes a deployment will make before execution, reducing the risk of unintended modifications.
- **Team accessibility**: Bicep is designed to be readable by developers who are not infrastructure specialists. The declarative syntax is more approachable than ARM templates or imperative scripts.

### Negative

- **Azure-only**: Bicep is an Azure-native DSL. It cannot manage resources in other cloud providers or on-premises infrastructure. If the architecture later requires multi-cloud or hybrid deployment, a separate IaC tool would be needed for non-Azure resources.
- **Learning curve**: Team members unfamiliar with declarative IaC must learn Bicep syntax, module structure, and deployment workflows. This is mitigated by Bicep's readability compared to alternatives.
- **Bootstrap dependency**: Some tenant-level resources (Entra ID app registrations, Key Vault firewall rules referencing team IPs) must be configured manually before Bicep deployment can run. These are documented as prerequisites.

## Alternatives Considered

### ARM Templates

- **Description**: Azure Resource Manager templates using JSON. The native Azure IaC format, supported by all Azure services.
- **Rejection Reason**: ARM templates are verbose, difficult to read, and prone to JSON syntax errors. Bicep compiles to ARM templates and provides the same deployment capabilities with a cleaner syntax, module support, and type safety.

### Terraform

- **Description**: A cloud-agnostic IaC tool using HashiCorp Configuration Language (HCL). Supports multiple cloud providers and on-premises infrastructure.
- **Rejection Reason**: Terraform adds a separate state management requirement (state file storage, locking, reconciliation) and a different deployment pipeline. ATLAS is Azure-only, and Bicep provides better Azure integration, native `what-if` validation, and a simpler toolchain. Terraform would be appropriate if multi-cloud support were required.

### Azure CLI Scripts

- **Description**: Imperative PowerShell or Bash scripts using `az` commands to create and configure resources.
- **Rejection Reason**: Imperative scripts are not idempotent by default. They require careful error handling, ordering, and dependency management to produce consistent results. Declarative Bicep templates describe the desired end state and let Azure Resource Manager determine the execution order.

### Manual Portal Configuration

- **Description**: Configuring all Azure resources through the Azure Portal web interface.
- **Rejection Reason**: Not reproducible, not auditable, not reviewable, and error-prone. Portal-configured infrastructure cannot be recovered from source control in a disaster scenario.

## References

- ADR-006: GitHub Actions (CI/CD pipeline that executes Bicep deployments)
- ADR-007: Bicep Infrastructure as Code (earlier tool selection decision, now formalized as a mandatory architectural principle)
- ADR-019: Azure App Service as the Primary Production Hosting Platform (primary resource provisioned by Bicep)
