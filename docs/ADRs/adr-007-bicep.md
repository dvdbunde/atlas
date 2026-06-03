---
title: "ADR-007: Use Bicep for Infrastructure as Code"
status: "Accepted"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["infrastructure", "bicep", "iac", "azure"]
supersedes: ""
superseded_by: ""
---

# ADR-007: Use Bicep for Infrastructure as Code

## Status

### Accepted

## Context

ATLAS requires Azure resources (App Service, SQL Database, Blob Storage, etc.) to be deployed consistently across environments (dev, staging, production). We need an Infrastructure as Code (IaC) approach that:

1. **Declarative syntax** - Define infrastructure in code, not manual portal clicks
2. **Azure-native** - First-class support for Azure resources and features
3. **Repeatable deployments** - Same configuration produces identical environments
4. **Version controlled** - Infrastructure changes tracked in Git alongside application code
5. **Integrates with CI/CD** - Deploy infrastructure via GitHub Actions (ADR-006)
6. **Supports modularity** - Reusable components for common patterns (App Service + SQL)

Alternative IaC tools considered:

- **Terraform** - Cloud-agnostic but requires separate tooling, HCL language learning curve
- **Azure Resource Manager (ARM) Templates** - Native to Azure but verbose JSON syntax, hard to maintain
- **Pulumi** - Uses general-purpose languages (C#, TypeScript) but additional tooling, smaller ecosystem
- **Azure CLI Scripts** - Imperative approach, hard to ensure idempotency, no dependency management

## Decision

We will use **Bicep** as the Infrastructure as Code (IaC) tool for ATLAS.

### Bicep Architecture

```text
┌──────────────────────────────────────────────────┐
│           GitHub Repository                      │
│                                                  │
│  infra/                                          │
│  ├── main.bicep              # Entry point       │
│  ├── modules/                                    │
│  │   ├── app-service.bicep   # Reusable module   │
│  │   ├── sql-database.bicep  # Reusable module   │
│  │   └── blob-storage.bicep  # Reusable module   │
│  ├── parameters/                                 │
│  │   ├── dev.json            # Dev environment   │
│  │   ├── staging.json        # Staging env       │
│  │   └── prod.json           # Production env    │
│  └── scripts/                                    │
│      └── deploy.ps1          # Deployment helper │
└──────────────┬───────────────────────────────────┘
               │ Deployed via GitHub Actions (ADR-006)
               │
┌──────────────▼───────────────────────────────────┐
│           Azure Resource Group                   │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  App Service Plan (Linux/Windows)          │  │
│  │  └── App Service (atlas-app-dev)           │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  SQL Server                                │  │
│  │  └── SQL Database (atlas-db-dev)           │  │
│  │      (Serverless tier, ADR-003)            │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  Storage Account                           │  │
│  │  └── Blob Container (permit-documents)     │  │
│  │      (Hot tier, ADR-003)                   │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  Application Insights                      │  │
│  │  Key Vault                                 │  │
│  │  Microsoft Entra ID (ADR-008)              │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### Bicep File Structure

```bicep
// infra/main.bicep (simplified example)
param environment string
param location string = 'westeurope'
param appServicePlanSku string = 'B1'

// Modules
module appService 'modules/app-service.bicep' = {
  name: 'appServiceDeploy'
  params: {
    environment: environment
    location: location
    sku: appServicePlanSku
  }
}

module sqlDatabase 'modules/sql-database.bicep' = {
  name: 'sqlDatabaseDeploy'
  params: {
    environment: environment
    location: location
    sku: 'GP_S_Gen5_2'  // Serverless, ADR-003
  }
}

module blobStorage 'modules/blob-storage.bicep' = {
  name: 'blobStorageDeploy'
  params: {
    environment: environment
    location: location
    sku: 'Standard_LRS'
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output sqlServerName string = sqlDatabase.outputs.sqlServerName
```

## Consequences

### Positive

1. **Azure-native** - First-class support from Azure, always up-to-date with new resource features
2. **Simpler than ARM** - Declarative syntax, ~60% less code than equivalent ARM JSON templates
3. **Modular and reusable** - Modules enable DRY (Don't Repeat Yourself) infrastructure code
4. **Dependency management** - Bicep automatically resolves resource dependencies (no manual ordering)
5. **What-if deployments** - Preview changes before applying (`az deployment group what-if`)
6. **GitHub Actions integration** - Deploy via `azure/arm-deploy` action (ADR-006)
7. **Visual Studio Code support** - Bicep extension with intellisense, validation, and snippets

### Negative

1. **Azure-only** - Not portable to AWS or GCP (acceptable since ATLAS is Azure-only per PRD)
2. **Newer language** - Smaller community compared to Terraform, fewer third-party modules
3. **State management** - Relies on Azure deployment history (no separate state file like Terraform)
4. **Learning curve** - Team must learn Bicep syntax (mitigated by VS Code extension and docs)

### Mitigations

- **Documentation** - Create `docs/engineering/infrastructure-guidelines.md` with Bicep examples
- **Modules library** - Build reusable modules for common ATLAS patterns (App Service + SQL + Blob)
- **Parameter validation** - Use `@description` and `@allowed` decorators for parameter validation
- **CI/CD integration** - Deploy via GitHub Actions (ADR-006) with what-if preview before apply
- **Training** - Team workshop on Bicep syntax and modular design patterns

## Alternatives Considered

### Terraform

- **ALT-001**: **Description**: Cloud-agnostic IaC tool using HCL language
- **ALT-002**: **Rejection Reason**: Additional tooling (Terraform CLI, state management), HCL learning curve, not Azure-native (though Azure provider is excellent), overkill for Azure-only project

### ARM Templates (JSON)

- **ALT-003**: **Description**: Native Azure JSON format for resource deployment
- **ALT-004**: **Rejection Reason**: Verbose syntax, no intellisense in VS Code, hard to maintain, Bicep compiles to ARM anyway (use Bicep directly)

### Pulumi

- **ALT-005**: **Description**: IaC using general-purpose languages (C#, TypeScript, Python)
- **ALT-006**: **Rejection Reason**: Additional tooling and ecosystem to manage, smaller community than Terraform or Bicep, not justified for ATLAS team size

### Azure CLI Scripts

- **ALT-007**: **Description**: Imperative bash/PowerShell scripts using `az` commands
- **ALT-008**: **Rejection Reason**: Hard to ensure idempotency, no dependency management, error-prone for complex deployments, not declarative

## Implementation Notes

- **IMP-001**: Create `infra/` directory with `main.bicep` and modular `modules/` subdirectory
- **IMP-002**: Define environment-specific parameter files (`parameters/dev.json`, `staging.json`, `prod.json`)
- **IMP-003**: Use Bicep modules for each major resource type (App Service, SQL, Blob, Key Vault)
- **IMP-004**: Integrate Bicep deployment into GitHub Actions `deploy.yml` (ADR-006)
- **IMP-005**: Configure SQL Database with Serverless tier (per ADR-003: Azure SQL decision)
- **IMP-006**: Use Microsoft Entra ID authentication for SQL (no SQL auth, per ADR-008)
- **IMP-007**: Store secrets (connection strings, keys) in Azure Key Vault, not in Bicep parameters

## Compliance with Requirements

| Requirement | How Bicep Addresses It |
| ----------- | --------------------- |
| PRD C-03: Azure SQL Database | ✅ Deploy SQL Server + Database with Serverless tier |
| PRD C-04: Azure Blob Storage | ✅ Deploy Storage Account + Blob container |
| PRD C-06: Azure App Service | ✅ Deploy App Service Plan + App Service |
| ADR-003: Data Storage Strategy | ✅ Bicep modules for SQL and Blob with correct SKUs |
| ADR-006: GitHub Actions | ✅ Deploy Bicep via `azure/arm-deploy` action |
| ADR-008: Microsoft Entra ID | ✅ Configure Entra ID authentication for App Service and SQL |
| PRD Constraint: West Europe | ✅ Bicep parameter for location (default: `westeurope`) |

## References

- **REF-001**: [ADR-003: Azure SQL + Blob Storage](adr-003-azure-sql-blob.md)
- **REF-002**: [ADR-006: GitHub Actions CI/CD](adr-006-github-actions.md)
- **REF-003**: [ADR-008: Microsoft Entra ID Authentication](adr-008-microsoft-entra-id.md)
- **REF-004**: [ATLAS PRD - Technology Stack](../PRDs/atlas-mvp-prd.md#technology-stack)
- **REF-005**: [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- **REF-006**: [Bicep Modules](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/modules)
- **REF-007**: [Deploy Bicep with GitHub Actions](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deploy-github-actions)
