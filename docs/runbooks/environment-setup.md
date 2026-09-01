# First Azure Deployment Runbook

## Purpose

This runbook describes the steps required to perform the first deployment of the ATLAS application into Azure.

The Azure infrastructure is provisioned using the project's Infrastructure-as-Code (Bicep) templates. This document covers the remaining manual configuration required before the GitHub Actions deployment pipeline can successfully build, deploy and validate the application.

The runbook is divided into three phases:

1. One-Time Repository Configuration
2. Post-Provisioning Azure Configuration
3. First Application Deployment & Validation

Only the second and third phases depend on Azure resources already existing.

---

## Prerequisites

Before starting, ensure the following prerequisites are met.

- Azure subscription available
- GitHub repository configured
- Azure CLI installed
- Sufficient Azure permissions (Owner or User Access Administrator)
- GitHub Actions enabled
- CI/CD workflows merged into the `main` branch

---

## Phase 1 – One-Time Repository Configuration

These steps only need to be performed once for a repository.

They are independent of the Azure resources and therefore remain valid even if the entire Azure Resource Group is later deleted and recreated.

---

### Step 1 – Configure GitHub OIDC Authentication

The GitHub Actions deployment workflows authenticate to Azure using **OpenID Connect (OIDC)**.

To separate deployment permissions from the application identities, ATLAS uses a dedicated Microsoft Entra App Registration for GitHub Actions.

The App Registrations used by the solution are:

| App Registration | Purpose |
| ------------------ | --------- |
| ATLAS GitHub Actions | GitHub Actions deployment (OIDC) |
| ATLAS API | API authentication and Blazor client authentication |
| ATLAS Swagger UI | Swagger OAuth2 authentication |

> **Note**
>
> The Blazor application reuses the **ATLAS API** App Registration and therefore does not require its own App Registration.

---

#### 1. Create the GitHub Actions App Registration

In the Azure Portal navigate to:

```txt
Microsoft Entra ID
    → App registrations
```

Select:

```txt
New registration
```

Configure:

| Setting | Value |
| ---------- | ------- |
| Name | ATLAS GitHub Actions |
| Supported account types | Accounts in this organizational directory only |
| Redirect URI | None |

Click **Register**.

---

#### 2. Record the Required Identifiers

After the App Registration has been created, record the following values.

| Property | Used for |
| ----------- | ---------- |
| Application (Client) ID | GitHub Repository Variable (`AZURE_CLIENT_ID`) |
| Directory (Tenant) ID | GitHub Repository Variable (`AZURE_TENANT_ID`) |

The Subscription ID can be obtained from:

```txt
Azure Portal
    → Subscriptions
```

This value will be stored as the `AZURE_SUBSCRIPTION_ID` Repository Variable.

---

#### 3. Create the Federated Credential

Inside the **ATLAS GitHub Actions** App Registration navigate to:

```txt
Certificates & secrets
    → Federated credentials
```

Select:

```txt
Add credential
```

Configure the credential as follows.

| Setting | Value |
| ---------- | ------- |
| Federated credential scenario | GitHub Actions deploying Azure resources |
| Organization | *Your GitHub username or organization* |
| Repository | *ATLAS repository* |
| Entity type | Branch |
| GitHub branch | `main` |
| Name | `github-main` |

Click **Add**.

This allows GitHub Actions workflows running from the `main` branch to authenticate to Azure without storing credentials or secrets.

---

#### 4. Grant Azure RBAC Permissions

The GitHub Actions deployment identity requires permission to create and manage Azure resources during infrastructure and application deployments.

Assign the **Contributor** role to the **ATLAS GitHub Actions** Enterprise Application at the **Azure Subscription** scope.

> **Why Subscription scope?**
>
> During the initial bootstrap, the deployment identity is responsible for creating the Resource Group and all Azure resources. Since the Resource Group does not yet exist, the role must be assigned at the subscription level.
>
> After the initial deployment, you may optionally reduce the scope to the Resource Group following the principle of least privilege.

##### Retrieve the Service Principal Object ID

The role assignment requires the **Enterprise Application Object ID** (service principal), not the Application (Client) ID.

Run:

```powershell
$appId = "<ATLAS GitHub Actions Application (Client) ID>"

$spObjectId = az ad sp show `
    --id $appId `
    --query id `
    --output tsv

Write-Host "Service Principal Object ID: $spObjectId"
```

The command should output the Object ID of the **ATLAS GitHub Actions** Enterprise Application.

##### Assign the Contributor Role

Run:

```powershell
$subscriptionId = "<Azure Subscription ID>"

az role assignment create `
    --assignee-object-id $spObjectId `
    --assignee-principal-type ServicePrincipal `
    --role Contributor `
    --scope "/subscriptions/$subscriptionId"
```

Expected output:

```text
"roleDefinitionName": "Contributor"
"principalType": "ServicePrincipal"
```

##### Verify the Role Assignment (Contributor)

Verify that the assignment was created successfully:

```powershell
az role assignment list `
    --assignee $spObjectId `
    --all
```

The output should include:

```text
Role: Contributor
Principal Type: ServicePrincipal
Scope: /subscriptions/<subscription-id>
```

> **Important**
>
> Although the Azure Portal documentation states that service principals can be selected from **Access control (IAM) → Add role assignment → Select members**, some Azure tenants only display users and groups in the member picker. The Azure CLI performs the same RBAC assignment reliably and is therefore recommended for this step.

---

> **Checkpoint**

Before continuing, verify:

- ✅ App Registration **ATLAS GitHub Actions** exists.
- ✅ Enterprise Application **ATLAS GitHub Actions** exists.
- ✅ Federated Credential has been created.
- ✅ Contributor role assigned successfully.
- ✅ `az role assignment list` shows the Contributor assignment.

---

#### 5. Assign Azure Container Registry Permissions

The deployment workflow publishes Docker images to Azure Container Registry.

Since the Azure Container Registry does not yet exist, the **AcrPush** role cannot be assigned during this phase.

After the infrastructure has been provisioned (Phase 2), assign the following role:

| Role | Scope |
| ------ | ------- |
| AcrPush | Azure Container Registry |

This role assignment cannot be performed until the Azure Container Registry has been provisioned during Phase 2.

---

#### 6. Verification

The configuration is complete when:

- [ ] ATLAS GitHub Actions App Registration exists
- [ ] Federated Credential has been created
- [ ] Contributor role assigned at the Subscription scope
- [ ] Application (Client) ID recorded
- [ ] Directory (Tenant) ID recorded
- [ ] Subscription ID recorded

The `AcrPush` role assignment will be completed after the Azure infrastructure has been provisioned during Phase 2.

---

### Step 2 – Configure GitHub Repository Variables

Configure the following Repository Variables.

| Variable | Description |
| ----------- | ------------- |
| AZURE_CLIENT_ID | Microsoft Entra Application Client ID |
| AZURE_SUBSCRIPTION_ID | Azure Subscription ID |
| AZURE_TENANT_ID | Microsoft Entra Tenant ID |
| DOTNET_VERSION *(optional)* | .NET SDK version used by CI |

These values are shared across all deployment environments and are not tied to any specific Azure resources.

---

### Step 3 – Configure GitHub Repository Secrets

Configure the following Repository Secrets.

| Secret | Description |
| --------- | ------------- |
| GITGUARDIAN_API_KEY | GitGuardian secret scanning |
| SNYK_TOKEN | Snyk vulnerability scanning |

These secrets are used during CI validation and are independent of the Azure infrastructure.

---

### Step 4 – Create the GitHub Environment

Create a GitHub Environment named:

`dev`

Optionally configure:

- Deployment approvals
- Required reviewers
- Deployment protection rules

This environment stores all deployment-specific configuration.

---

### Step 5 – Configure Dev Environment Variables

Configure the following variables inside the **dev** GitHub Environment.

| Variable | Description |
| ----------- | ------------- |
| ACR_NAME | Azure Container Registry name |
| AZURE_RESOURCE_GROUP | Resource Group name |
| API_APP_NAME | API App Service name |
| BLAZOR_APP_NAME | Blazor App Service name |
| API_BASE_URL | Public API URL |
| BLAZOR_BASE_URL | Public Blazor URL |

These values should correspond to the resource names defined by the Bicep templates.

Because the project uses deterministic naming, these variables can be configured before the infrastructure is provisioned.

The Bicep templates expose apiImageTag and blazorImageTag deployment parameters. During the first deployment these default to latest. Future CI/CD deployments should supply immutable image tags (for example the Git commit SHA) when invoking the Bicep deployment.

---

### Phase 1 Checklist

- [ ] GitHub OIDC authentication configured
- [ ] Repository Variables configured
- [ ] Repository Secrets configured
- [ ] `dev` GitHub Environment created
- [ ] Dev Environment Variables configured

At this point the repository is fully prepared for deployment.

The remaining work depends on the Azure infrastructure existing.

## Phase 2 – Post-Provisioning Azure Configuration

These steps are performed after the Azure infrastructure has been successfully provisioned using the Bicep templates.

Unlike the previous phase, these steps depend on Azure resources already existing.

If the Resource Group is ever deleted and recreated, this entire phase should be repeated.

---

### Step 6 – Deploy the Azure Infrastructure

If the target Resource Group does not already exist, create it before continuing.

```powershell
az group create `
    --name atlas-dev-rg `
    --location westeurope
```

Deploy the Infrastructure-as-Code (Bicep) templates.

```powershell
az deployment group create `
    --resource-group atlas-dev-rg `
    --template-file .\infra\main.bicep `
    --parameters .\infra\main.parameters.dev.json
```

If required, supply additional secure parameters (such as the SQL administrator password) during deployment.

Verify that the deployment completes successfully.

The deployment should provision the following resources:

- Azure Container Registry
- Linux App Service Plan
- Azure App Service (API)
- Azure App Service (Blazor)
- Azure SQL Server
- Azure SQL Database
- Azure Storage Account
- Azure Key Vault
- Application Insights
- Log Analytics Workspace
- System Assigned Managed Identity (API App Service)
- System Assigned Managed Identity (Blazor App Service)

Resolve any deployment errors before continuing.

---

### Step 7 – Execute the Infrastructure Bootstrap

After the Azure infrastructure has been successfully provisioned, execute the infrastructure bootstrap script.

The bootstrap script performs all required post-deployment Azure configuration and validation before the first GitHub Actions deployment.

Run:

```powershell
$GitHubClientId = "<ATLAS GitHub Actions Application (Client) ID>"

.\infra\bootstrap-revised.ps1 `
    -ResourceGroup atlas-dev-rg `
    -DeploymentName main `
    -GitHubClientId $GitHubClientId `
    -Environment dev
```

The bootstrap script automatically performs the following tasks:

- Assigns the **AcrPush** role to the **ATLAS GitHub Actions** Service Principal on the Azure Container Registry (if not already assigned).
- Verifies that the API and Blazor App Services have **System Assigned Managed Identities**.
- Verifies that both App Services have the **AcrPull** role assignment on the Azure Container Registry.
- Verifies the Azure SQL Server, SQL Database and SQL Administrator configuration.
- Verifies that the **AllowAzureServices** SQL firewall rule exists.
- Verifies that all required Azure infrastructure resources have been provisioned successfully.
- Produces a deployment summary showing the validation results for each Azure resource.

**Azure Communication Services (email) configuration:**

- Assigns the **Communication and Email Service Owner** role to both App Services (system-assigned Managed Identities) on the ACS Email Service.
- Assigns the **Storage Blob Data Contributor** role to the developer identity on the Storage Account (for email-template administration).
- Discovers the **Azure-managed sender domain** (`AzureManagedDomain`) and the `DoNotReply` sender username, then resolves the sender address (for example `DoNotReply@<domain>.azurecomm.net`).
- Sets the resolved sender address as the `Email__Acs__SenderAddress` app setting on both the API and Blazor App Services.
- Validates that both App Services have a valid `Email__Acs__SenderAddress` and that the ACS Email Service is provisioned successfully.

The script is fully **idempotent** and can safely be executed after every infrastructure deployment. Existing role assignments are detected and will not be recreated.

Resolve any reported failures before continuing.

---

### Step 8 – Configure the SQL Connection String

Create the following GitHub Environment Secret.

| Secret | Description |
| --------- | ------------- |
| SQL_CONNECTION_STRING | Azure SQL connection string |

Retrieve the connection string from:

>**Azure Portal → Azure SQL Database → Connection strings → ADO.NET**

Replace the placeholder username and password with the SQL administrator credentials created during infrastructure deployment.

The deployment workflow uses this connection string to execute EF Core database migrations.

```text
Server=tcp:<sql-server>.database.windows.net,1433;
Initial Catalog=<database>;
Persist Security Info=False;
User ID=<sql-admin>;
Password=<password>;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

> **Important**
>
> Store this value as a **GitHub Environment Secret**, not as a Repository Secret.

---

### Phase 2 Checklist

- [ ] Azure infrastructure successfully deployed
- [ ] Infrastructure bootstrap completed successfully
- [ ] GitHub Actions `AcrPush` permission configured
- [ ] App Service Managed Identities verified
- [ ] Azure Container Registry permissions verified
- [ ] Azure SQL configuration verified
- [ ] Azure SQL firewall verified
- [ ] SQL connection string configured as the `SQL_CONNECTION_STRING` GitHub Environment Secret

At this point, the Azure environment is fully configured and ready for the first application deployment.

---

## Phase 3 – First Application Deployment & Validation

With both the repository and Azure environment fully configured, the first application deployment can now be performed.

This phase validates that the complete CI/CD pipeline functions correctly and that the deployed application is operational.

---

### Step 9 – Execute the Deployment Pipeline

After the Pull Request has been approved, merged into the `main` branch and the CI validation has completed successfully, verify that the deployment workflows execute automatically in the following order:

```text
01-package.yml
    ↓
02-deploy-dev.yml
    ↓
03-smoke-tests.yml
```

Each workflow should complete successfully before the next workflow begins.

Verify that:

- Docker images are built successfully
- Docker images are pushed to Azure Container Registry
- API App Service is updated
- Blazor App Service is updated
- EF Core database migrations execute successfully
- Smoke tests complete successfully

---

### Step 10 – Verify the Deployed Applications

After deployment, verify that both applications are accessible.

#### API

```txt
https://<api-app>.azurewebsites.net/swagger
```

Verify:

- API starts successfully
- Swagger UI loads
- Protected endpoints require authentication
- Health endpoint responds successfully

#### Blazor

```txt
https://<blazor-app>.azurewebsites.net
```

Verify:

- Application starts successfully
- Authentication works correctly
- API communication succeeds
- No browser console errors are present

#### Email (Azure Communication Services)

Verify:

- Both App Services have the `Email__Acs__SenderAddress` app setting set to a valid ACS sender (for example `DoNotReply@<domain>.azurecomm.net`)
- Both App Services have the **Communication and Email Service Owner** role on the ACS Email Service
- Both App Services have the **Storage Blob Data Contributor** role on the Storage Account (for email-template persistence)
- A status change (for example an application submission or approval) triggers an email notification delivered via ACS

---

### Step 11 – Verify Database Migration

Confirm that the EF Core migrations executed successfully.

Verify:

- Database exists
- Tables have been created
- Latest migration appears in the `__EFMigrationsHistory` table
- Seed data has been inserted (if applicable)

---

### Step 12 – Verify Application Insights

Open the Application Insights resource.

Verify that telemetry is being collected.

Confirm that:

- Requests are recorded
- Dependencies are recorded
- Exceptions are recorded
- Logs are available
- Live Metrics (if enabled) are functioning

---

### Step 13 – Verify Smoke Tests

Confirm that the `03-smoke-tests.yml` workflow completed successfully.

Typical validation includes:

- API health endpoint
- HTTP response codes
- Blazor availability
- Basic endpoint validation

Investigate and resolve any failures before considering the deployment successful.

---

## Troubleshooting

### EF Core Migration Failed

Possible causes include:

- Missing `SQL_CONNECTION_STRING`
- Incorrect SQL administrator credentials
- Azure SQL firewall blocking the GitHub runner
- Database unavailable

---

### App Service Failed to Start

Possible causes include:

- Missing application settings
- Container image unavailable
- Incorrect container configuration
- Startup failure

Check the App Service logs and container logs for further details.

---

### Container Image Could Not Be Pulled

Verify that the Managed Identity assigned to the App Service has the `AcrPull` role assignment on the Azure Container Registry.

---

### Smoke Tests Failed

Verify:

- Deployment completed successfully
- API is responding
- Blazor application is accessible
- Health endpoints are functioning

---

### Authentication Failed

Verify:

- Microsoft Entra configuration
- Redirect URIs
- Client IDs
- Application settings
- App Registration configuration

---

## Final Deployment Checklist

### Phase 1 – Repository Configuration

- [ ] GitHub OIDC authentication configured
- [ ] Repository Variables configured
- [ ] Repository Secrets configured
- [ ] `dev` GitHub Environment created
- [ ] Dev Environment Variables configured

### Phase 2 – Azure Configuration

- [ ] Azure infrastructure deployed
- [ ] SQL connection string configured
- [ ] Azure SQL firewall verified
- [ ] SQL administrator verified
- [ ] `AcrPull` permissions verified
- [ ] App Service configuration verified
- [ ] ACS sender address configured (`Email__Acs__SenderAddress`)
- [ ] ACS Email Service Owner role assigned to both App Services
- [ ] Storage Blob Data Contributor role assigned (email-template persistence)

## Phase 3 – Deployment Validation

- [ ] Pull Request merged into `main`
- [ ] `01-package.yml` completed successfully
- [ ] `02-deploy-dev.yml` completed successfully
- [ ] `03-smoke-tests.yml` completed successfully
- [ ] API reachable
- [ ] Swagger accessible
- [ ] Blazor application accessible
- [ ] Database migrations applied
- [ ] Application Insights receiving telemetry

---

## Expected Outcome

Upon completion of this runbook:

- The Azure infrastructure has been successfully provisioned.
- The GitHub repository is fully configured for automated deployments.
- GitHub Actions authenticates to Azure using OpenID Connect (OIDC).
- Docker images are automatically built and published to Azure Container Registry.
- The ATLAS API and Blazor applications are successfully deployed.
- The Azure SQL Database schema is up to date.
- Smoke tests validate the deployment.
- Application Insights is collecting telemetry.
- Email notifications are delivered via Azure Communication Services using the configured sender address.
- Future deployments require only pushing changes to the `main` branch.

---

## Repeating a Deployment

If the Azure Resource Group is deleted and recreated, only **Phase 2** and **Phase 3** need to be repeated.

The GitHub repository configuration performed during **Phase 1** remains valid and does not need to be recreated unless the repository itself changes.
