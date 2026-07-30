# Milestone 9 – Infrastructure Design Specification

**Version:** 1.0
**Status:** Approved Design Specification
**Milestone:** 9 – Cloud Enablement & Production Deployment
**Phase:** Infrastructure Refactoring

---

## 1. Purpose

This document defines the target Azure infrastructure architecture for the ATLAS platform.

It serves as the implementation specification for the Infrastructure-as-Code (IaC) project and supersedes any implicit assumptions made by the current Bicep implementation.

The objective is to align the IaC implementation with the deployment architecture described in the First Azure Deployment Runbook while preserving the existing modular Bicep structure and coding standards.

This document intentionally focuses on infrastructure architecture and does not describe deployment procedures.

---

## 2. Objectives

The infrastructure shall:

- support fully automated deployments
- support GitHub Actions using OpenID Connect (OIDC)
- deploy all infrastructure using Bicep only
- require no manual Azure Portal configuration after bootstrap
- support independent API and Blazor deployments
- support containerized applications
- support Managed Identity
- support Azure SQL
- support Key Vault
- support Application Insights
- support future production environments
- remain fully parameterized
- remain idempotent

---

## 3. Architectural Principles

### Infrastructure as Code First

Every Azure resource shall be provisioned through Bicep.

Manual Azure Portal configuration is considered an exception and must be documented in the deployment runbook.

---

### Modular Design

Each Azure resource type shall remain encapsulated inside its own reusable Bicep module.

The existing module structure shall be preserved wherever practical.

---

### Deterministic Naming

Infrastructure names shall be deterministic.

Deploying the same environment multiple times shall produce identical resource names.

---

### Separation of Responsibilities

Infrastructure modules are responsible only for infrastructure provisioning.

Deployment workflows are responsible only for deploying application artifacts.

Application configuration remains inside the application projects.

---

### Environment Independence

The same templates shall deploy:

- Development
- Test
- Acceptance
- Production

using parameter files only.

---

## 4. Target Azure Architecture

The final infrastructure shall provision:

- Resource Group
- App Service Plan
- API App Service
- Blazor App Service
- Azure Container Registry
- Azure SQL Server
- Azure SQL Database
- Storage Account
- Key Vault
- Application Insights
- Log Analytics Workspace
- Managed Identities
- Role Assignments

---

## 5. Resource Review

## Resource Group

### Status

Existing

### Required Changes

None.

---

## App Service Plan

### Status

Existing

### Required Changes

Verify SKU.

No architectural changes expected.

---

## API App Service

### Current

Single generic App Service.

### Target

Dedicated Linux Web App for Containers.

Purpose:

- hosts ATLAS API
- exposes Swagger
- exposes Health endpoint

### Required Features

- Linux
- Container deployment
- Managed Identity
- Application Insights
- Key Vault references
- Health Check Path
- HTTPS only

---

## Blazor App Service

### Current

Missing.

### Target

Dedicated Linux Web App for Containers.

Purpose:

- hosts Blazor WebAssembly application

### Required Features

- Linux
- Container deployment
- Managed Identity
- Application Insights
- HTTPS only

---

## Azure Container Registry

### Current

Missing.

### Target

One Azure Container Registry.

Purpose:

- stores API image
- stores Blazor image

### Required Features

- Admin user disabled
- Azure AD authentication
- Managed Identity support

---

## Azure SQL Server

### Status

Existing.

Review configuration only.

---

## Azure SQL Database

### Status

Existing.

Review configuration only.

---

## Storage Account

### Status

Existing.

Review configuration only.

---

## Key Vault

### Status

Existing.

Verify:

- RBAC model
- Managed Identity access
- Secret references

---

## Application Insights

### Status

Existing.

Verify:

- Workspace-based
- Connected to Log Analytics

---

## Log Analytics Workspace

### Status

Existing.

No architectural changes expected.

---

## Managed Identity

### Current

Mixed implementation.

User Assigned Managed Identity exists.

App Service uses System Assigned Identity.

### Decision

Use **System Assigned Managed Identities** for App Services.

Remove the User Assigned Managed Identity unless a future requirement justifies its existence.

---

## 6. Naming Convention

The naming module shall expose explicit resource names.

Instead of:

```txt
appServiceName
```

the naming module shall expose:

```txt
resourceGroupName

apiAppServiceName

blazorAppServiceName

containerRegistryName

storageAccountName

sqlServerName

sqlDatabaseName

keyVaultName

applicationInsightsName

logAnalyticsWorkspaceName
```

---

## App Service Names

API

```txt
atlas-api-dev
```

Blazor

```txt
atlas-blazor-dev
```

These names intentionally produce predictable URLs.

---

## Resource Group

```
atlas-dev-rg
```

---

## Azure Container Registry

```
atlas-acr-pxto
```

---

# 7. GitHub Environment Compatibility

The following GitHub Environment Variables shall remain valid.

| Variable | Expected Value |
|------------|----------------|
| ACR_NAME | atlas-acr-pxto |
| AZURE_RESOURCE_GROUP | atlas-dev-rg |
| API_APP_NAME | atlas-api-dev |
| API_BASE_URL | https://atlas-api-dev.azurewebsites.net |
| BLAZOR_APP_NAME | atlas-blazor-dev |
| BLAZOR_BASE_URL | https://atlas-blazor-dev.azurewebsites.net |

The IaC shall be modified to match these values.

The GitHub Environment shall not require changes.

---

# 8. Required Module Changes

## naming.bicep

### Add

- apiAppServiceName
- blazorAppServiceName
- containerRegistryName

Remove the generic App Service naming abstraction.

---

## appservice.bicep

Convert into a reusable Linux Container App Service module.

The module shall support:

- application name
- App Service Plan
- Managed Identity
- container image
- Application Insights
- Health Check Path
- App Settings
- Key Vault references

The module shall be instantiated twice.

---

## containerregistry.bicep

New module.

Responsibilities:

- Azure Container Registry
- login server output
- resource ID output

---

## roleassignments.bicep

Extend to include:

GitHub Actions

- Contributor
- AcrPush

API App Service

- AcrPull

Blazor App Service

- AcrPull

---

## outputs.bicep

Add outputs for:

- apiAppName
- blazorAppName
- apiHostname
- blazorHostname
- acrName
- acrLoginServer

---

# 9. Main Deployment Changes

main.bicep shall:

deploy

- Resource Group
- App Service Plan
- API App Service
- Blazor App Service
- Azure Container Registry
- Azure SQL
- Storage
- Key Vault
- Application Insights
- Log Analytics

configure

- dependencies
- role assignments
- outputs

---

# 10. Managed Identity Strategy

GitHub Actions

Authentication

OpenID Connect

Permissions

Contributor

AcrPush

---

API App Service

Identity

System Assigned

Permissions

AcrPull

Key Vault Secrets User (future)

---

Blazor App Service

Identity

System Assigned

Permissions

AcrPull

Key Vault Secrets User (future)

---

# 11. Deployment Flow

The infrastructure shall support the following deployment workflow.

```
GitHub Actions

↓

Build Docker Images

↓

Push Images to Azure Container Registry

↓

Deploy API Container

↓

Deploy Blazor Container

↓

Execute EF Core Migrations

↓

Run Smoke Tests
```

No manual deployment steps shall be required.

---

# 12. Outputs

The deployment shall expose outputs for:

```
Resource Group

API App Service Name

API Hostname

Blazor App Service Name

Blazor Hostname

Azure Container Registry Name

Azure Container Registry Login Server

Storage Account Name

SQL Server Name

Database Name

Key Vault Name

Application Insights Connection String
```

Outputs shall provide all information required by deployment workflows.

---

# 13. Acceptance Criteria

The infrastructure implementation is complete when:

- Resource Group deployed
- App Service Plan deployed
- API App Service deployed
- Blazor App Service deployed
- Azure Container Registry deployed
- Azure SQL deployed
- Storage Account deployed
- Key Vault deployed
- Application Insights deployed
- Log Analytics deployed
- Managed Identities configured
- Role assignments configured
- All deployment outputs available
- GitHub Environment variables correspond to deployed resources
- Bicep deployment is idempotent
- No manual Azure resource creation required

---

# 14. Non-Goals

This refactoring does **not** include:

- Deployment workflow implementation
- EF Core migrations
- Application configuration changes
- Monitoring dashboards
- Azure Alerts
- Production networking
- Private Endpoints
- Front Door
- Application Gateway
- WAF
- Backup configuration

These concerns are addressed in later phases of Milestone 9.

---

# 15. Implementation Strategy

The implementation shall proceed in the following order.

1. Refactor the naming module.
2. Add the Azure Container Registry module.
3. Refactor the App Service module for Linux container hosting.
4. Provision separate API and Blazor App Services.
5. Configure Managed Identities.
6. Configure AcrPull and AcrPush role assignments.
7. Update deployment outputs.
8. Validate deterministic naming.
9. Execute a clean Bicep deployment.
10. Execute the First Azure Deployment Runbook.

The infrastructure shall not be considered complete until the deployment runbook executes successfully without requiring undocumented manual intervention.