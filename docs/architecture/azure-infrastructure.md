# ATLAS Azure Foundation

This document describes the Azure Foundation established in **Milestone 9 – Phase 1**.

## Phase Scope

Milestone 9 – Phase 1 establishes the core Azure infrastructure required to host the ATLAS platform. The objective of this milestone is to create a solid, repeatable Infrastructure-as-Code (IaC) foundation rather than a fully production-hardened Azure platform.

The implemented infrastructure is intentionally focused on the minimum set of Azure resources required to support application development, testing, and future deployment automation.

Since Milestone 9 – Phase 1, the following capabilities have been implemented and are documented in this file:

- Key Vault secrets (SQL connection string stored in Key Vault; App Services use Key Vault references)
- Continuous Deployment (GitHub Actions workflows build and deploy both App Services)
- Monitoring & Diagnostics (Milestone 11 – O2: Log Analytics, Application Insights, diagnostic settings, Azure Managed Grafana — see [Observability & Monitoring](#observability--monitoring-milestone-11--o2))
- Managed Identity via **system-assigned** identities on the App Services (used for Azure Communication Services, Blob Storage, ACR pull, and Key Vault access)

Remaining future infrastructure work:

- Azure SQL authentication using Microsoft Entra ID (current implementation uses SQL administrator authentication)
- Deployment Slots
- Production networking (Private Endpoints / VNet integration)
- Production hardening
- Monitoring alerts and action groups (O7)

## Overview

This document describes the Infrastructure-as-Code foundation for the ATLAS permit processing system on Microsoft Azure.

The infrastructure follows the architectural principles documented in:

- ADR-019: Azure App Service as the Primary Production Hosting Platform
- ADR-020: Infrastructure as Code Using Bicep
- ADR-021: Environment-Based Configuration and Secret Management
- ADR-022: Managed Azure Services over Self-Hosted Infrastructure
- ADR-023: Production Observability Strategy

The Azure Foundation follows several core design principles:

- Infrastructure as Code using Bicep
- Modular resource architecture
- Centralized naming and tagging
- Environment-specific parameterization
- Repeatable and idempotent deployments
- Managed Azure services over self-hosted infrastructure
- No manual Azure Portal configuration after the initial bootstrap

All deployments are intentionally **idempotent**. Re-running the deployment reconciles Azure resources to the desired state without requiring manual intervention or recreating existing resources.

## Folder Structure

```text
infra/
├── main.bicep                    # Entry point — orchestrates all modules
├── main.parameters.dev.json      # Development environment parameters
└── modules/
    ├── names.bicep               # Central naming convention
    ├── tags.bicep                # Central resource tagging
    ├── appserviceplan.bicep      # App Service Plan
    ├── appservice.bicep          # App Service (Web App) — used for both API and Blazor
    ├── containerregistry.bicep   # Azure Container Registry
    ├── sqlserver.bicep           # Azure SQL Server
    ├── sqldatabase.bicep         # Azure SQL Database
    ├── storage.bicep             # Storage Account (Blob) — documents + email templates
    ├── communicationservices.bicep # Azure Communication Services (email)
    ├── keyvault.bicep            # Azure Key Vault
    ├── loganalytics.bicep        # Log Analytics Workspace
    ├── appinsights.bicep         # Application Insights
    ├── grafana.bicep             # Azure Managed Grafana (O2)
    └── diagnosticsettings.bicep  # Reusable diagnostic settings module (O2)
```

## Observability & Monitoring (Milestone 11 – O2)

The O2 phase establishes the Azure telemetry foundation. All monitoring
infrastructure is deployed through Bicep; no manual portal configuration is
required.

### Telemetry topology

```text
API App Service ──────┐
Blazor App Service ───┤
SQL Database ─────────┤  diagnostic settings   ┌─────────────────────┐
Storage (Blob) ───────┼───────────────────────►│ Log Analytics       │◄── Application
Key Vault ────────────┤                        │ (atlas-{env}-logs)  │    Insights
ACS ──────────────────┘                        └──────────▲──────────┘    (workspace-backed)
                                                          │ KQL + metrics (RBAC)
                                              ┌───────────┴───────────┐
                                              │ Managed Grafana       │
                                              │ (atlas-{env}-grafana) │
                                              └───────────────────────┘
```

- **Application Insights** (`atlas{env}appi`) is workspace-based and linked to
  the central Log Analytics workspace. Both App Services receive the
  connection string via `APPLICATIONINSIGHTS_CONNECTION_STRING`. No
  instrumentation-key configuration is used.
- **Diagnostic settings** route curated platform log and metric categories from
  each resource to the Log Analytics workspace. Categories are deliberately
  curated for operational value versus ingestion cost — e.g. App Service HTTP
  logs are excluded because Application Insights already captures request
  telemetry, and Blob Storage diagnostics are configured on the account's
  `blobServices/default` child resource where those categories are exposed.
- **Azure Managed Grafana** is the primary operational dashboard platform.
  Dashboards themselves are a later phase (O5/O6); O2 delivers the provisioned,
  authorized instance only.

### Grafana identity and RBAC

Grafana uses a **system-assigned managed identity** — no API keys, passwords,
or stored credentials. Two built-in role assignments grant read access to
monitoring data:

| Role | Role definition ID | Scope | Purpose |
|---|---|---|---|
| Monitoring Reader | `43d0d8ad-25c7-4714-9337-8ba259a9fe05` | ATLAS resource group | Metric/list access across all ATLAS resources |
| Log Analytics Reader | `73c42c96-874c-492b-b04d-ab87d138a893` | Log Analytics workspace | KQL queries against workspace tables |

Role assignment names use deterministic `guid()` seeds so re-deployments are
idempotent. After deployment, Grafana's Azure Monitor data source authenticates
via this managed identity automatically; no post-deployment credential setup is
required to query data (building dashboards on top remains O5/O6 work).

### Bootstrap verification

`infra/bootstrap.ps1` Phase 11 verifies the O2 foundation against live Azure:
workspace health, Application Insights workspace linkage and connection-string
configuration, existence and destination of every diagnostic setting, Grafana
provisioning state, and both Grafana role assignments at their expected scopes.

## Bootstrap

The Resource Group is intentionally **not** created by the Bicep templates.

The infrastructure deployment targets an existing Resource Group (`targetScope = 'resourceGroup'`), following the common Azure practice of separating **subscription-scoped provisioning** from **resource-group-scoped application infrastructure**.

This keeps the Infrastructure-as-Code focused on the application resources while allowing subscription-level resources (Resource Groups, Policies, Management Groups, etc.) to be managed independently.

The bootstrap process therefore consists of a single manual step:

```powershell
az group create `
    --name atlas-dev-rg `
    --location westeurope
```

After the Resource Group has been created, **all remaining infrastructure is managed exclusively through Bicep**.

## Prerequisites

- Azure CLI installed
- Bicep CLI installed (included with recent Azure CLI versions)
- Authenticated Azure CLI session
- Sufficient Azure subscription permissions (Contributor at subscription or resource group scope)
- Target Azure subscription selected

Authenticate:

```powershell
az login
```

Select the target subscription:

```powershell
az account set `
    --subscription "<subscription-id-or-name>"
```

## Azure Resources

### Resource Group

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| Resource Group | atlas-{env}-rg | atlas-dev-rg |

### Compute

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| App Service Plan | atlas-{env}-plan | atlas-dev-plan |
| App Service (API) | atlas-api-{env}-{suffix} | atlas-api-dev-{suffix} |
| App Service (Blazor) | atlas-blazor-{env}-{suffix} | atlas-blazor-dev-{suffix} |
| Container Registry | atlasacr{suffix} | atlasacr{suffix} |

### Database

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| SQL Server | atlas{env}sql | atlasdevsql |
| SQL Database | atlas-{env}-db | atlas-dev-db |

### Storage

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| Storage Account | atlas{env}storage | atlasdevstorage |
| Blob Container (documents) | permit-documents | permit-documents |
| Blob Container (email templates) | email-templates | email-templates |

### Security

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| Key Vault | atlas{env}kv | atlasdevkv |

### Messaging / Email

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| Communication Services | atlas-comm-{env}-{suffix} | atlas-comm-dev-{suffix} |
| Email Service | atlas-comm-{env}-{suffix}-email | atlas-comm-dev-{suffix}-email |
| Email Domain | AzureManagedDomain | AzureManagedDomain |

### Identity

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| Managed Identity | System-assigned on each App Service and on Managed Grafana | System-assigned |

### Observability

| Resource | Name Pattern | Dev Value |
| ---------- | ------------- | ----------- |
| Log Analytics Workspace | atlas-{env}-logs | atlas-dev-logs |
| Application Insights | atlas{env}appi | atlasdevappi |
| Azure Managed Grafana | atlas-{env}-grafana | atlas-dev-grafana |

## Naming Convention

All resource names are derived from the central naming module (`modules/names.bicep`). The convention follows the pattern `atlas-{environment}-{resource-type}`, with exceptions for globally unique resources (Storage Accounts, SQL Servers and Key Vaults), which use a compact naming format.

> **Rule:** Never hard-code Azure resource names in modules or `main.bicep`. Always reference outputs from the **names** module.

## Tagging Strategy

Every deployed resource receives the following tags:

| Tag | Value |
| ----- | ------- |
| Project | ATLAS |
| Environment | Derived from Environment parameter |
| ManagedBy | Bicep |
| Repository | ATLAS |
| Owner | Engineering |

Tags are defined centrally in `modules/tags.bicep` and applied consistently to every deployed resource.

## Parameter Files

### Development (`main.parameters.dev.json`)

```json
{
  "environment": "dev",
  "location": "westeurope",
  "uniqueSuffix": "",
  "sqlAdminLogin": "atlasadmin",
  "appServicePlanSkuTier": "Basic",
  "appServicePlanSkuSize": "B1",
  "appServicePlanCapacity": 1,
  "sqlDatabaseSkuName": "GP_S_Gen5",
  "sqlDatabaseCapacity": 1,
  "sqlDatabaseAutoPauseDelay": 15,
  "sqlDatabaseMaxSizeBytes": 34359738368,
  "storageSku": "Standard_LRS",
  "logAnalyticsRetentionInDays": 30,
  "enableResourceLock": false
}
```

### Future Environments

To add a new environment (test, staging or production):

1. Copy `main.parameters.dev.json` to `main.parameters.{env}.json`
2. Update the environment-specific parameter values (SKUs, capacity, naming prefix, etc.)
3. Deploy using the new parameter file

## Deployment Workflow

The recommended deployment workflow is:

1. Authenticate with Azure.
2. Select the target Azure subscription.
3. Create the Resource Group (first deployment only).
4. Validate the Bicep template.
5. Review the deployment using **What-If**.
6. Deploy the infrastructure.
7. Verify the deployment outputs.

Following this workflow helps detect configuration issues before deployment and ensures infrastructure changes remain predictable and repeatable.

## Deployment Commands

### Login to Azure

```powershell
az login
```

### Select Subscription

```powershell
az account set `
    --subscription "<subscription-id-or-name>"
```

### Create Resource Group

> This step is only required once per environment.

```powershell
az group create `
    --name atlas-dev-rg `
    --location westeurope
```

### Validate Deployment

Validation performs template validation without deploying any resources.

```powershell
az deployment group validate `
    --resource-group atlas-dev-rg `
    --template-file .\infra\main.bicep `
    --parameters .\infra\main.parameters.dev.json
```

### Review Planned Changes (What-If)

Always run **What-If** before deploying infrastructure changes.

```powershell
az deployment group what-if `
    --resource-group atlas-dev-rg `
    --template-file .\infra\main.bicep `
    --parameters .\infra\main.parameters.dev.json
```

### Deploy Infrastructure

```powershell
az deployment group create `
    --resource-group atlas-dev-rg `
    --template-file .\infra\main.bicep `
    --parameters .\infra\main.parameters.dev.json
```

Because the deployment is **idempotent**, this command can safely be executed multiple times. Existing resources are updated only when configuration changes are detected.

### Destroy Infrastructure

```powershell
az group delete `
    --name atlas-dev-rg `
    --yes `
    --no-wait
```

> ⚠️ Deleting the Resource Group permanently removes **all** Azure resources contained within it, including the SQL database and Blob Storage. Ensure any required data has been backed up before deleting the environment.

## Outputs

After deployment, the following outputs are available:

| Output | Description |
| -------- | ----------- |
| resourceGroupName | Name of the Resource Group |
| apiAppServiceName | Name of the API App Service |
| apiAppServiceResourceId | ARM resource ID of the API App Service |
| apiHostname | Default hostname of the API App Service |
| apiPrincipalId | Principal ID of the API App Service managed identity |
| blazorAppServiceName | Name of the Blazor App Service |
| blazorAppServiceResourceId | ARM resource ID of the Blazor App Service |
| blazorHostname | Default hostname of the Blazor App Service |
| blazorPrincipalId | Principal ID of the Blazor App Service managed identity |
| containerRegistryName | Name of the Azure Container Registry |
| containerRegistryLoginServer | Login server of the Azure Container Registry |
| containerRegistryResourceId | ARM resource ID of the Azure Container Registry |
| appServicePlanName | Name of the App Service Plan |
| sqlServerName | Name of the SQL Server |
| sqlServerFqdn | Fully qualified domain name of the SQL Server |
| sqlDatabaseName | Name of the SQL Database |
| storageAccountName | Name of the Storage Account |
| storagePrimaryBlobEndpoint | Primary Blob Storage endpoint |
| keyVaultName | Name of the Key Vault |
| keyVaultUri | URI of the Key Vault |
| keyVaultTenantId | Microsoft Entra tenant identifier |
| applicationInsightsName | Name of the Application Insights resource |
| applicationInsightsConnectionString | Application Insights connection string |
| logAnalyticsWorkspaceName | Name of the Log Analytics Workspace |
| logAnalyticsWorkspaceId | ARM resource ID of the Log Analytics Workspace |
| grafanaName | Name of the Azure Managed Grafana instance |
| grafanaEndpoint | Endpoint URL of the Azure Managed Grafana instance |
| grafanaPrincipalId | Principal ID of the Grafana managed identity |
| communicationServiceName | Name of the ACS Communication Service |
| communicationServicesEndpoint | ACS endpoint URL |
| communicationEmailServiceName | Name of the ACS Email Service |

These outputs are consumed by `infra/bootstrap.ps1` for post-deployment
verification and are available for deployment automation.

## Troubleshooting

### Deployment Fails with Role Assignment Error

Ensure your Azure CLI session has **Contributor** (or **Owner**) permissions at the Resource Group or Subscription scope.

Verify the currently selected subscription:

```powershell
az account show
```

If necessary, switch to the correct subscription:

```powershell
az account set `
    --subscription "<subscription-id-or-name>"
```

### Bicep Build Fails

Before deploying, verify that the Bicep templates compile successfully:

```powershell
az bicep build `
    --file .\infra\main.bicep
```

Any compilation errors should be resolved before attempting a deployment. Bicep reports the file name and line number for each error.

### Resource Already Exists

Bicep deployments are **idempotent**.

If an existing resource already matches the desired configuration, the deployment succeeds without making changes.

If Azure reports a conflict, compare the deployed configuration with the Bicep templates and resolve the configuration drift before redeploying.

### Key Vault Name Conflict

Key Vault names must be globally unique across Azure.

If the generated name is unavailable, adjust the naming convention (for example by modifying the generated suffix in `modules/names.bicep`) before redeploying.

### What-If Shows Unexpected Changes

Always review the output of:

```powershell
az deployment group what-if `
    --resource-group atlas-dev-rg `
    --template-file .\infra\main.bicep `
    --parameters .\infra\main.parameters.dev.json
```

Unexpected changes generally indicate one of the following:

- manual changes made through the Azure Portal
- parameter drift between environments
- modifications to the Bicep templates
- updated default values

Investigate unexpected changes before executing the deployment.

### Deployment Can Be Safely Re-run

Infrastructure deployments are intentionally **idempotent**.

Re-running the deployment:

- does **not** recreate existing resources
- updates only resources whose configuration has changed
- reconciles Azure resources with the desired state defined in the Bicep templates

This makes repeated deployments safe during development and forms the basis for future CI/CD automation.

---

## Phase Completion Checklist

- [x] Infrastructure folder created (`infra/`)
- [x] Modular Bicep architecture implemented
- [x] `main.bicep` orchestrates all modules
- [x] Development parameter file created
- [x] Naming convention centralized in `modules/names.bicep`
- [x] Tagging centralized in `modules/tags.bicep`
- [x] Azure resource naming standardized
- [x] Bicep templates build successfully (`az bicep build`)
- [x] Infrastructure deployment validated
- [x] Deployment is idempotent
- [x] No Azure Portal configuration required after bootstrap
- [x] Azure Foundation documentation completed

---

## Next Steps

The Azure Foundation established during **Milestone 9 – Phase 1**, together with
the O2 Azure Monitor integration, provides the platform for the remaining
infrastructure work.

Genuinely remaining infrastructure evolution includes:

- Azure SQL authentication using Microsoft Entra ID
- Deployment Slots
- Production networking (Private Endpoints / VNet integration)
- Production hardening
- Monitoring alerts and action groups (Milestone 11 – O7)

The modular Bicep architecture established during this milestone is intended to support future enhancements without requiring significant restructuring of the infrastructure code.

## Metrics (Milestone 11 – O4)

ATLAS emits application metrics via `System.Diagnostics.Metrics` under the
meter name `ATLAS.Application` (same identity as the O3 ActivitySource). They
are exported through the same OpenTelemetry → Azure Monitor pipeline as traces,
and only when an Application Insights connection string is configured — locally
the instruments simply have no listener.

### Instruments

| Metric | Type | Dimensions | Emission boundary |
|---|---|---|---|
| `atlas.applications.transitions` | Counter<long> | `transition` (created, submitted, approved, rejected, info_requested, resubmitted — fixed set of 6) | Application command handlers, once per successful business transition |
| `atlas.email.sends` | Counter<long> | `outcome` (success, failure) | `AcsEmailService.SendAsync`, exactly once per send attempt |
| `atlas.email.duration` | Histogram<double> (ms) | `outcome` (success, failure) | Same boundary as above |
| `atlas.command.duration` | Histogram<double> (ms) | `command` (MediatR command type name — bounded by the number of command types) | `TracingBehavior`, around every command execution |

### Conventions

- **Cardinality**: dimensions are strictly low-cardinality. ApplicationId,
  UserId, DocumentId, email addresses and blob names are never used as
  dimensions; those values live in logs/traces where per-event analysis is
  possible without unbounded time series.
- **Separation of concerns**: metrics answer "what is happening repeatedly?",
  logs answer "what happened in this event?", traces answer "what happened in
  this operation?", and the business Audit Log remains the permanent business
  history. None replace another.
- **No duplication of Azure-native telemetry**: Blob Storage operations are not
  custom-instrumented because Azure Monitor already provides storage metrics;
  HTTP request metrics remain with classic Application Insights.
