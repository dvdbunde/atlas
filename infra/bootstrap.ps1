<#
.SYNOPSIS
    Bootstraps and validates the ATLAS development Azure environment.

.DESCRIPTION
    Performs post-deployment configuration and validation for the ATLAS
    development environment defined by the Bicep infrastructure.

    The script:
      - Reads deployment outputs from the completed Bicep deployment.
      - Resolves the GitHub Actions Service Principal.
      - Configures and verifies ACR permissions for GitHub Actions.
      - Verifies managed identities for the API and Blazor App Services.
      - Verifies ACR pull permissions for both App Services.
      - Verifies Key Vault RBAC configuration and application identity access.
      - Verifies Storage Account RBAC configuration and application identity access.
      - Verifies App Service configuration, including Key Vault references
        for the SQL connection string.
      - Verifies the SQL Server firewall configuration.
      - Verifies that all expected Azure resources exist.
      - Prints a final deployment validation summary.

    This script does not deploy the Azure infrastructure itself.
    Run main.bicep first with the appropriate environment parameters.

    The script is intended for the ATLAS development environment.

.PARAMETER ResourceGroupName
    Name of the Azure Resource Group containing the ATLAS development
    environment.

.PARAMETER DeploymentName
    Name of the ARM/Bicep deployment whose outputs are used by this script.

.EXAMPLE
    .\bootstrap.ps1 `
        -ResourceGroupName "atlas-dev-rg" `
        -DeploymentName "main"

.NOTES
    Prerequisites:
      - Azure CLI installed and available on PATH.
      - Authenticated Azure CLI session with access to the target subscription.
      - main.bicep deployment completed successfully.
      - Appropriate permissions to inspect and configure the Azure resources.

    The script validates Azure resource configuration and RBAC assignments.
    It does not grant the executing developer account access to application
    secrets in Key Vault unless explicitly configured to do so elsewhere.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]$ResourceGroup,

    [Parameter(Mandatory)]
    [string]$DeploymentName,

    [Parameter(Mandatory)]
    [string]$GitHubClientId
)

$ErrorActionPreference = 'Stop'

# --------------------------------------------------------------------------
# Exit codes
# --------------------------------------------------------------------------
$EXIT_SUCCESS            = 0
$EXIT_DEPLOYMENT         = 1
$EXIT_AUTHENTICATION     = 2
$EXIT_SERVICEPRINCIPAL   = 3
$EXIT_INFRASTRUCTURE    = 4
$EXIT_PREREQUISITES     = 5

# --------------------------------------------------------------------------
# Constants
# --------------------------------------------------------------------------
$AcrPushRoleId = '8311e382-0749-4cb8-b61a-304f252e45ec'
$AcrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
$StorageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
$KeyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
$CommunicationEmailServiceOwnerRoleId = '09976791-48a7-449e-bb21-39d1a415f350'

# --------------------------------------------------------------------------
# Helper functions
# --------------------------------------------------------------------------
function Write-Step {
    param([string]$Message)
    Write-Host "`n>>> $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "  PASS $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "  FAIL $Message" -ForegroundColor Red
}

function Write-Skip {
    param([string]$Message)
    Write-Host "  SKIP $Message" -ForegroundColor Yellow
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message,
        [int]$ExitCode = $EXIT_INFRASTRUCTURE
    )
    if (-not $Condition) {
        Write-Fail $Message
        exit $ExitCode
    }
}

# --------------------------------------------------------------------------
# Prerequisites – Validate Azure CLI and Azure Login
# --------------------------------------------------------------------------
function Assert-AzureCliInstalled {
    Write-Step "Prerequisite – Verifying Azure CLI"

    $azVersion = az version 2>$null | ConvertFrom-Json
    if (-not $azVersion) {
        Write-Fail "Azure CLI is not installed or not on PATH. Install Azure CLI from https://aka.ms/install-azure-cli and try again."
        exit $EXIT_PREREQUISITES
    }

    Write-Pass "Azure CLI $($azVersion.'azure-cli') detected"
}

function Assert-AzureLogin {
    Write-Step "Prerequisite – Verifying Azure login"

    $account = az account show --output json 2>$null | ConvertFrom-Json
    if (-not $account) {
        Write-Fail "Not authenticated to Azure. Run 'az login' (and optionally 'az account set --subscription <id>') before running this script."
        exit $EXIT_AUTHENTICATION
    }

    Write-Pass "Authenticated as $($account.user.name) (subscription: $($account.name))"
}

# --------------------------------------------------------------------------
# Phase 1 – Load Deployment Outputs
# --------------------------------------------------------------------------
function Get-DeploymentOutputs {
    param([string]$ResourceGroup, [string]$DeploymentName)

    Write-Step "Phase 1 – Loading deployment outputs"

    $result = az deployment group show `
        --resource-group $ResourceGroup `
        --name $DeploymentName `
        --output json 2>$null | ConvertFrom-Json

    if (-not $result) {
        Write-Fail "Deployment '$DeploymentName' not found in resource group '$ResourceGroup'."
        exit $EXIT_DEPLOYMENT
    }

    if ($result.properties.provisioningState -ne 'Succeeded') {
        Write-Fail "Deployment '$DeploymentName' has status '$($result.properties.provisioningState)'. Expected 'Succeeded'."
        exit $EXIT_DEPLOYMENT
    }

    $outputs = $result.properties.outputs
    if (-not $outputs) {
        Write-Fail "Deployment '$DeploymentName' has no outputs."
        exit $EXIT_DEPLOYMENT
    }

    Write-Pass "Deployment located and succeeded"

    return [PSCustomObject]@{
        resourceGroupName          = $outputs.resourceGroupName.value
        apiAppServiceName          = $outputs.apiAppServiceName.value
        apiAppServiceResourceId    = $outputs.apiAppServiceResourceId.value
        apiHostname                = $outputs.apiHostname.value
        apiPrincipalId             = $outputs.apiPrincipalId.value
        blazorAppServiceName       = $outputs.blazorAppServiceName.value
        blazorAppServiceResourceId = $outputs.blazorAppServiceResourceId.value
        blazorHostname             = $outputs.blazorHostname.value
        blazorPrincipalId          = $outputs.blazorPrincipalId.value
        containerRegistryName      = $outputs.containerRegistryName.value
        containerRegistryLoginServer = $outputs.containerRegistryLoginServer.value
        containerRegistryResourceId  = $outputs.containerRegistryResourceId.value
        appServicePlanName         = $outputs.appServicePlanName.value
        sqlServerName              = $outputs.sqlServerName.value
        sqlServerFqdn              = $outputs.sqlServerFqdn.value
        sqlDatabaseName            = $outputs.sqlDatabaseName.value
        storageAccountName         = $outputs.storageAccountName.value
        storageAccountResourceId   = "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroup/providers/Microsoft.Storage/storageAccounts/$($outputs.storageAccountName.value)"
        storagePrimaryBlobEndpoint = $outputs.storagePrimaryBlobEndpoint.value
        keyVaultName               = $outputs.keyVaultName.value
        keyVaultUri                = $outputs.keyVaultUri.value
        applicationInsightsName    = $outputs.applicationInsightsName.value
        applicationInsightsConnectionString = $outputs.applicationInsightsConnectionString.value
        logAnalyticsWorkspaceName  = $outputs.logAnalyticsWorkspaceName.value
        logAnalyticsWorkspaceId    = $outputs.logAnalyticsWorkspaceId.value
        operationsWorkbookName     = $outputs.operationsWorkbookName.value
        actionGroupName            = $outputs.actionGroupName.value
        apiAvailabilityAlertName   = $outputs.apiAvailabilityAlertName.value
        blazorAvailabilityAlertName = $outputs.blazorAvailabilityAlertName.value
        exceptionSpikeAlertName    = $outputs.exceptionSpikeAlertName.value
        emailFailureAlertName      = $outputs.emailFailureAlertName.value
        commandLatencyAlertName    = $outputs.commandLatencyAlertName.value
        serviceHealthAlertName     = $outputs.serviceHealthAlertName.value
        operationsGrafanaDashboardName = $outputs.operationsGrafanaDashboardName.value
        grafanaName                = $outputs.grafanaName.value
        grafanaEndpoint            = $outputs.grafanaEndpoint.value
        grafanaPrincipalId         = $outputs.grafanaPrincipalId.value
        communicationServiceName = $outputs.communicationServiceName.value
        communicationEmailServiceName = $outputs.communicationEmailServiceName.value    
        communicationServiceResourceId = "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroup/providers/Microsoft.Communication/communicationServices/$($outputs.communicationServiceName.value)"
    }
}

# --------------------------------------------------------------------------
# Phase 2 – Resolve GitHub Service Principal
# --------------------------------------------------------------------------
function Get-GitHubServicePrincipal {
    param([string]$ClientId)

    Write-Step "Phase 2 – Resolving GitHub Service Principal"

    $sp = az ad sp show --id $ClientId --output json 2>$null | ConvertFrom-Json
    if (-not $sp) {
        Write-Fail "GitHub Service Principal not found for Client ID '$ClientId'."
        exit $EXIT_SERVICEPRINCIPAL
    }

    Write-Pass "GitHub Service Principal resolved (Object ID: $($sp.id))"
    return $sp
}

# --------------------------------------------------------------------------
# Phase 3a – Configure GitHub AcrPush
# --------------------------------------------------------------------------
function Ensure-AcrPushRoleAssignment {
    param(
        [string]$PrincipalId,
        [string]$Scope,
        [string]$PrincipalName,
        [string]$KeyVaultName
    )

    Write-Step "Phase 3a – Configuring AcrPush for $PrincipalName"

    # ----------------------------------------------------------------------
    # GitHub Actions -> Key Vault Secrets User
    # ----------------------------------------------------------------------

    # Resolve the Key Vault resource ID from Azure rather than constructing
    # it manually. This avoids invalid-scope errors caused by missing or
    # stale subscription/resource-name variables.
    $KeyVaultResourceId = az keyvault show `
        --name $KeyVaultName `
        --resource-group $ResourceGroup `
        --query id `
        --output tsv 2>$null

    Assert-True ($null -ne $KeyVaultResourceId -and $KeyVaultResourceId.Trim() -ne '') `
        "Key Vault '$KeyVaultName' could not be resolved."

    $githubKeyVaultRole = az role assignment list `
        --assignee-object-id $PrincipalId `
        --scope $KeyVaultResourceId `
        --role $KeyVaultSecretsUserRoleId `
        --output json 2>$null | ConvertFrom-Json

    if ($null -eq $githubKeyVaultRole -or @($githubKeyVaultRole).Count -eq 0) {

        az role assignment create `
            --assignee-object-id $PrincipalId `
            --assignee-principal-type ServicePrincipal `
            --role $KeyVaultSecretsUserRoleId `
            --scope $KeyVaultResourceId `
            --output none

        Assert-True ($LASTEXITCODE -eq 0) `
            "Failed to assign Key Vault Secrets User to $PrincipalName."

        Write-Pass "GitHub Actions has Key Vault Secrets User"
    }
    else {
        Write-Pass "GitHub Actions already has Key Vault Secrets User"
    }

    # ----------------------------------------------------------------------
    # GitHub Actions -> AcrPush
    # ----------------------------------------------------------------------

    $existing = az role assignment list `
        --assignee-object-id $PrincipalId `
        --scope $Scope `
        --role $AcrPushRoleId `
        --output json 2>$null | ConvertFrom-Json

    if ($null -ne $existing -and @($existing).Count -gt 0) {
        Write-Pass "AcrPush already assigned to $PrincipalName"
        return
    }

    az role assignment create `
        --assignee-object-id $PrincipalId `
        --assignee-principal-type ServicePrincipal `
        --role $AcrPushRoleId `
        --scope $Scope `
        --output none

    Assert-True ($LASTEXITCODE -eq 0) `
        "Failed to assign AcrPush to $PrincipalName."

    # Immediate verification
    $verify = az role assignment list `
        --assignee-object-id $PrincipalId `
        --scope $Scope `
        --role $AcrPushRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $verify -and @($verify).Count -gt 0) `
        "AcrPush role assignment for $PrincipalName could not be verified."

    Write-Pass "AcrPush assigned to $PrincipalName"
}

# --------------------------------------------------------------------------
# Phase 3b – Configure Developer Storage Access
# --------------------------------------------------------------------------
function Ensure-DeveloperStorageAccess {
    param(
        [string]$StorageAccountResourceId
    )

    Write-Step "Phase 3b – Configuring developer Blob Storage access"

    $currentUser = az ad signed-in-user show --output json | ConvertFrom-Json

    $assignment = az role assignment list `
        --assignee $currentUser.id `
        --scope $StorageAccountResourceId `
        --role $StorageBlobDataContributorRoleId `
        --output json | ConvertFrom-Json

    if ($null -ne $assignment -and @($assignment).Count -gt 0) {
        Write-Pass "Developer already has Storage Blob Data Contributor"
        return
    }

    az role assignment create `
    --assignee-object-id $currentUser.id `
    --assignee-principal-type User `
    --role $StorageBlobDataContributorRoleId `
    --scope $StorageAccountResourceId `
    --output none

    $verify = az role assignment list `
        --assignee $currentUser.id `
        --scope $StorageAccountResourceId `
        --role $StorageBlobDataContributorRoleId `
        --output json | ConvertFrom-Json

    Assert-True ($null -ne $verify -and @($verify).Count -gt 0) `
        "Developer Storage Blob Data Contributor role could not be verified."

    Write-Pass "Developer granted Storage Blob Data Contributor"
}

# --------------------------------------------------------------------------
# Phase 3c – Configure Azure Communication Services permissions
# --------------------------------------------------------------------------
function Ensure-AcsPermissions {
    param(
        [Parameter(Mandatory)]
        [string]$CommunicationServiceResourceId,

        [Parameter(Mandatory)]
        [string]$ApiPrincipalId,

        [Parameter(Mandatory)]
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 3c – Configuring Azure Communication Services permissions"

    foreach ($app in @(
        [PSCustomObject]@{
            Name = "API App Service"
            PrincipalId = $ApiPrincipalId
        },
        [PSCustomObject]@{
            Name = "Blazor App Service"
            PrincipalId = $BlazorPrincipalId
        }
    )) {
        $existing = az role assignment list `
            --assignee-object-id $app.PrincipalId `
            --scope $CommunicationServiceResourceId `
            --role $CommunicationEmailServiceOwnerRoleId `
            --output json 2>$null | ConvertFrom-Json

        if ($null -eq $existing -or @($existing).Count -eq 0) {
            az role assignment create `
                --assignee-object-id $app.PrincipalId `
                --assignee-principal-type ServicePrincipal `
                --role $CommunicationEmailServiceOwnerRoleId `
                --scope $CommunicationServiceResourceId `
                --output none

            Assert-True ($LASTEXITCODE -eq 0) `
                "Failed to assign Communication and Email Service Owner to $($app.Name)."

            Write-Pass "$($app.Name) granted Communication and Email Service Owner"
        }
        else {
            Write-Pass "$($app.Name) already has Communication and Email Service Owner"
        }

        # Immediate verification
        $verify = az role assignment list `
            --assignee-object-id $app.PrincipalId `
            --scope $CommunicationServiceResourceId `
            --role $CommunicationEmailServiceOwnerRoleId `
            --output json 2>$null | ConvertFrom-Json

        Assert-True ($null -ne $verify -and @($verify).Count -gt 0) `
            "$($app.Name) Communication and Email Service Owner role could not be verified."
    }
}

# --------------------------------------------------------------------------
# Phase 4a – Verify Managed Identities
# --------------------------------------------------------------------------
function Verify-ManagedIdentities {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName
    )

    Write-Step "Phase 4a – Verifying Managed Identities"

    foreach ($appName in @($ApiAppName, $BlazorAppName)) {
        $app = az webapp show `
            --name $appName `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json

        Assert-True ($null -ne $app) "App Service '$appName' not found."

        $identity = $app.identity
        Assert-True ($null -ne $identity) "App Service '$appName' has no Managed Identity."
        Assert-True ($identity.type -eq 'SystemAssigned') "App Service '$appName' does not use SystemAssigned identity."

        Write-Pass "$appName has SystemAssigned Managed Identity (Principal ID: $($identity.principalId))"
    }
}

# --------------------------------------------------------------------------
# Phase 4b – Configure App Service ACR Pull Authentication
# --------------------------------------------------------------------------
function Ensure-AppServiceAcrPullConfiguration {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName
    )

    Write-Step "Phase 4b – Configuring App Service ACR pull authentication"

    foreach ($appName in @($ApiAppName, $BlazorAppName)) {
        az webapp config set `
            --name $appName `
            --resource-group $ResourceGroup `
            --acr-use-identity true `
            --acr-identity '[system]' `
            --output none

        Assert-True ($LASTEXITCODE -eq 0) `
            "Failed to configure managed identity ACR pull authentication for '$appName'."

        $config = az webapp config show `
            --name $appName `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json

        Assert-True ($null -ne $config) `
            "Unable to read ACR pull configuration for '$appName'."
        Assert-True ($config.acrUseManagedIdentityCreds -eq $true) `
            "App Service '$appName' is not configured to use managed identity for ACR pulls."
        Assert-True ([string]::IsNullOrWhiteSpace($config.acrUserManagedIdentityID)) `
            "App Service '$appName' unexpectedly has a user-assigned ACR identity configured."

        Write-Pass "$appName uses SystemAssigned Managed Identity for ACR pulls"
    }
}

# --------------------------------------------------------------------------
# Phase 5 – Configure and Verify ACR Permissions
# --------------------------------------------------------------------------
function Verify-AcrPermissions {
    param(
        [string]$AcrResourceId,
        [string]$GitHubPrincipalId,
        [string]$ApiPrincipalId,
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 5 – Verifying ACR permissions"

    # GitHub AcrPush
    $ghPush = az role assignment list `
        --assignee $GitHubPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPushRoleId `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($null -ne $ghPush -and @($ghPush).Count -gt 0) `
        "GitHub Service Principal missing AcrPush on ACR."
    Write-Pass "GitHub AcrPush"

    # API AcrPull
    $apiPull = az role assignment list `
        --assignee-object-id $ApiPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPullRoleId `
        --output json 2>$null | ConvertFrom-Json

    if ($null -eq $apiPull -or @($apiPull).Count -eq 0) {
        az role assignment create `
            --assignee-object-id $ApiPrincipalId `
            --assignee-principal-type ServicePrincipal `
            --role $AcrPullRoleId `
            --scope $AcrResourceId `
            --output none

        Assert-True ($LASTEXITCODE -eq 0) `
            "Failed to assign AcrPull to API App Service."
    }

    $apiPull = az role assignment list `
        --assignee-object-id $ApiPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPullRoleId `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($null -ne $apiPull -and @($apiPull).Count -gt 0) `
        "API App Service AcrPull on ACR could not be verified."
    Write-Pass "API AcrPull"

    # Blazor AcrPull
    $blazorPull = az role assignment list `
        --assignee-object-id $BlazorPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPullRoleId `
        --output json 2>$null | ConvertFrom-Json

    if ($null -eq $blazorPull -or @($blazorPull).Count -eq 0) {
        az role assignment create `
            --assignee-object-id $BlazorPrincipalId `
            --assignee-principal-type ServicePrincipal `
            --role $AcrPullRoleId `
            --scope $AcrResourceId `
            --output none

        Assert-True ($LASTEXITCODE -eq 0) `
            "Failed to assign AcrPull to Blazor App Service."
    }

    $blazorPull = az role assignment list `
        --assignee-object-id $BlazorPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPullRoleId `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($null -ne $blazorPull -and @($blazorPull).Count -gt 0) `
        "Blazor App Service AcrPull on ACR could not be verified."
    Write-Pass "Blazor AcrPull"
}

# --------------------------------------------------------------------------
# Phase 6 – Verify Key Vault Integration
# --------------------------------------------------------------------------
function Verify-KeyVaultIntegration {
    param(
        [string]$KeyVaultName,
        [string]$GitHubPrincipalId,
        [string]$ApiPrincipalId,
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 6 – Verifying Key Vault integration"

    # Key Vault exists and uses RBAC authorization
    $vault = az keyvault show `
        --name $KeyVaultName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $vault) "Key Vault '$KeyVaultName' not found."
    Assert-True ($vault.properties.enableRbacAuthorization -eq $true) `
        "Key Vault '$KeyVaultName' does not use RBAC authorization."

    Write-Pass "Key Vault exists and uses RBAC authorization"

    # GitHub Actions -> Key Vault Secrets User
    $githubKeyVaultRole = az role assignment list `
        --assignee $GitHubPrincipalId `
        --scope $vault.id `
        --role $KeyVaultSecretsUserRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $githubKeyVaultRole -and @($githubKeyVaultRole).Count -gt 0) `
        "GitHub Actions is missing Key Vault Secrets User."

    Write-Pass "GitHub Actions has Key Vault Secrets User"

    # API App Service -> Key Vault Secrets User
    $apiRole = az role assignment list `
        --assignee $ApiPrincipalId `
        --scope $vault.id `
        --role $KeyVaultSecretsUserRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $apiRole -and @($apiRole).Count -gt 0) `
        "API App Service is missing Key Vault Secrets User role."

    Write-Pass "API App Service has Key Vault Secrets User"

    # Blazor App Service -> Key Vault Secrets User
    $blazorRole = az role assignment list `
        --assignee $BlazorPrincipalId `
        --scope $vault.id `
        --role $KeyVaultSecretsUserRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $blazorRole -and @($blazorRole).Count -gt 0) `
        "Blazor App Service is missing Key Vault Secrets User role."

    Write-Pass "Blazor App Service has Key Vault Secrets User" 

    # Required SQL secret
    # Verify the secret resource through Azure Resource Manager rather than
    # reading the secret value. This does not require the developer account
    # to have Key Vault Secrets User permissions.
    $sqlSecretResource = az resource show `
        --ids "$($vault.id)/secrets/sql-connection-string" `
        --api-version "2023-07-01" `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $sqlSecretResource) `
        "Key Vault secret 'sql-connection-string' does not exist."

    Write-Pass "SQL connection string secret exists"

    # Obsolete storage connection string must not exist anymore.
    # Query the secret resource through Azure Resource Manager rather than
    # attempting to read its value.
    $storageSecretResource = az resource show `
        --ids "$($vault.id)/secrets/storage-connection-string" `
        --api-version "2023-07-01" `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -eq $storageSecretResource) `
        "Obsolete Key Vault secret 'storage-connection-string' still exists."

    Write-Pass "Storage connection string secret is absent"
}

# --------------------------------------------------------------------------
# Phase 7 – Verify Storage Managed Identity Integration
# --------------------------------------------------------------------------
function Verify-StorageManagedIdentity {
    param(
        [string]$StorageAccountResourceId,
        [string]$ApiPrincipalId,
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 7 – Verifying Storage Managed Identity integration"

    # API App Service -> Storage Blob Data Contributor
    $apiRole = az role assignment list `
        --assignee $ApiPrincipalId `
        --scope $StorageAccountResourceId `
        --role $StorageBlobDataContributorRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $apiRole -and @($apiRole).Count -gt 0) `
        "API App Service is missing Storage Blob Data Contributor."

    Write-Pass "API App Service has Storage Blob Data Contributor"

    # Blazor App Service -> Storage Blob Data Contributor
    $blazorRole = az role assignment list `
        --assignee $BlazorPrincipalId `
        --scope $StorageAccountResourceId `
        --role $StorageBlobDataContributorRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($null -ne $blazorRole -and @($blazorRole).Count -gt 0) `
        "Blazor App Service is missing Storage Blob Data Contributor."

    Write-Pass "Blazor App Service has Storage Blob Data Contributor"
}

# --------------------------------------------------------------------------
# Phase 8a – Configure Azure Communication Services sender address
# --------------------------------------------------------------------------
function Configure-AcsSenderAddress {
    param(
        [Parameter(Mandatory)]
        [string]$ResourceGroup,

        [Parameter(Mandatory)]
        [string]$EmailServiceName,

        [Parameter(Mandatory)]
        [string]$ApiAppName,

        [Parameter(Mandatory)]
        [string]$BlazorAppName
    )

    Write-Step "Configuring Azure Communication Services sender address"

    #
    # Resolve the Azure-managed sender domain
    #
    $domain = az communication email domain show `
        --resource-group $ResourceGroup `
        --email-service-name $EmailServiceName `
        --domain-name AzureManagedDomain `
        --query "fromSenderDomain" `
        -o tsv

    Assert-True (![string]::IsNullOrWhiteSpace($domain)) `
        "Unable to resolve Azure Communication Services managed sender domain."

    Write-Pass "Managed sender domain: $domain"

    #
    # Resolve the DoNotReply sender username
    #
    $username = az communication email domain sender-username show `
        --resource-group $ResourceGroup `
        --email-service-name $EmailServiceName `
        --domain-name AzureManagedDomain `
        --sender-username DoNotReply `
        --query "username" `
        -o tsv

    Assert-True (![string]::IsNullOrWhiteSpace($username)) `
        "Unable to resolve Azure Communication Services sender username."

    Write-Pass "Sender username: $username"

    #
    # Build the sender address
    #
    $senderAddress = "$username@$domain"

    Write-Pass "Resolved sender address: $senderAddress"

    #
    # Local helper
    #
    function Update-AppServiceSenderAddress {
        param(
            [Parameter(Mandatory)]
            [string]$AppName
        )

        $currentSender = az webapp config appsettings list `
            --resource-group $ResourceGroup `
            --name $AppName `
            --query "[?name=='Email__Acs__SenderAddress'].value | [0]" `
            -o tsv

        if ($currentSender -eq $senderAddress) {
            Write-Pass "$AppName already configured."
            return
        }

        az webapp config appsettings set `
            --resource-group $ResourceGroup `
            --name $AppName `
            --settings Email__Acs__SenderAddress="$senderAddress" `
            --only-show-errors | Out-Null

        Write-Pass "$AppName updated."

        az webapp restart `
            --resource-group $ResourceGroup `
            --name $AppName `
            --only-show-errors | Out-Null

        Write-Pass "$AppName restarted."
    }

    #
    # Configure both App Services
    #
    Update-AppServiceSenderAddress -AppName $ApiAppName
    Update-AppServiceSenderAddress -AppName $BlazorAppName
}

# --------------------------------------------------------------------------
# Phase 8b – Verify App Service Configuration
# --------------------------------------------------------------------------
function Verify-AppServiceConfiguration {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName,
        [string]$KeyVaultName
    )

    Write-Step "Phase 8 - Verifying App Service configuration"

    foreach ($appName in @($ApiAppName, $BlazorAppName)) {

        $settings = az webapp config appsettings list `
            --name $appName `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json

        Assert-True ($null -ne $settings) `
            "Unable to read App Service settings for '$appName'."

        $settingMap = @{}

        foreach ($setting in $settings) {
            $settingMap[$setting.name] = $setting.value
        }

        # Required common settings
        Assert-True (-not [string]::IsNullOrWhiteSpace($settingMap['ASPNETCORE_ENVIRONMENT'])) `
            "$appName is missing ASPNETCORE_ENVIRONMENT."

        Assert-True (-not [string]::IsNullOrWhiteSpace($settingMap['APPLICATIONINSIGHTS_CONNECTION_STRING'])) `
            "$appName is missing APPLICATIONINSIGHTS_CONNECTION_STRING."

        Assert-True (-not [string]::IsNullOrWhiteSpace($settingMap['KeyVault__VaultName'])) `
            "$appName is missing KeyVault__VaultName."

        Assert-True (-not [string]::IsNullOrWhiteSpace($settingMap['Storage__AccountName'])) `
            "$appName is missing Storage__AccountName."

        # Azure Communication Services
        Assert-True (-not [string]::IsNullOrWhiteSpace($settingMap['Email__Acs__Endpoint'])) `
            "$appName is missing Email__Acs__Endpoint."

        Assert-True (-not [string]::IsNullOrWhiteSpace($settingMap['Email__Acs__SenderAddress'])) `
            "$appName is missing Email__Acs__SenderAddress."

        Assert-True `
            ($settingMap['Email__Acs__SenderAddress'] -match '^DoNotReply@.+\.azurecomm\.net$') `
            "$appName Email__Acs__SenderAddress is not a valid Azure Communication Services sender."

        # SQL connection string must be a Key Vault reference
        $sqlSetting = $settingMap['ConnectionStrings__DefaultConnection']

        Assert-True (-not [string]::IsNullOrWhiteSpace($sqlSetting)) `
            "$appName is missing ConnectionStrings__DefaultConnection."

        $expectedSqlSecretUri =
            "https://$KeyVaultName.vault.azure.net/secrets/sql-connection-string/"

        Assert-True `
            ($sqlSetting -match '^@Microsoft\.KeyVault\(SecretUri=(.+)\)$') `
            "$appName ConnectionStrings__DefaultConnection must be a Key Vault SecretUri reference."

        $actualSqlSecretUri = $Matches[1].TrimEnd('/')

        Assert-True `
            ($actualSqlSecretUri -ieq $expectedSqlSecretUri.TrimEnd('/')) `
            "$appName ConnectionStrings__DefaultConnection must reference sql-connection-string."

        Write-Host "  PASS ConnectionStrings__DefaultConnection references sql-connection-string"

        # Storage connection string must no longer exist
        Assert-True (-not $settingMap.ContainsKey('Storage__ConnectionString')) `
            "$appName still contains obsolete Storage__ConnectionString."

        Write-Pass "$appName App Service configuration is valid"
    }
}

# --------------------------------------------------------------------------
# Phase 9 – Verify SQL
# --------------------------------------------------------------------------
function Verify-SqlInfrastructure {
    param(
        [string]$SqlServerName,
        [string]$SqlDatabaseName
    )

    Write-Step "Phase 9 – Verifying SQL infrastructure"

    # SQL Server exists
    $server = az sql server show `
        --name $SqlServerName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($null -ne $server) "SQL Server '$SqlServerName' not found."
    Write-Pass "SQL Server"

    # SQL Administrator configured
    Assert-True (-not [string]::IsNullOrWhiteSpace($server.administratorLogin)) "SQL Administrator not configured."
    Write-Pass "SQL Administrator"

    # SQL Database exists
    $db = az sql db show `
        --name $SqlDatabaseName `
        --server $SqlServerName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($null -ne $db) "SQL Database '$SqlDatabaseName' not found."
    Write-Pass "Database"

    # SQL Firewall – AllowAzureServices
    $fwRules = az sql server firewall-rule list `
        --server $SqlServerName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    $allowAzure = $fwRules | Where-Object { $_.name -eq 'AllowAzureServices' -or $_.startIpAddress -eq '0.0.0.0' -and $_.endIpAddress -eq '0.0.0.0' }
    Assert-True ($null -ne $allowAzure -and @($allowAzure).Count -gt 0) `
        "SQL Firewall rule 'AllowAzureServices' (0.0.0.0 – 0.0.0.0) not found."
    Write-Pass "SQL Firewall"
}

# --------------------------------------------------------------------------
# Phase 10 – Verify Infrastructure Resources
# --------------------------------------------------------------------------
function Verify-InfrastructureResources {
    param(
        [string]$ContainerRegistryName,
        [string]$AppServicePlanName,
        [string]$ApiAppServiceName,
        [string]$BlazorAppServiceName,
        [string]$StorageAccountName,
        [string]$KeyVaultName,
        [string]$ApplicationInsightsName,
        [string]$LogAnalyticsWorkspaceName,
        [string]$SqlServerName,
        [string]$SqlDatabaseName,
        [string]$CommunicationServiceName,
        [string]$CommunicationEmailServiceName
    )

    Write-Step "Phase 10 – Verifying infrastructure resources"

    Write-Host "Resource Group : $ResourceGroup"
    Write-Host "CommunicationServiceName : $CommunicationServiceName"
    Write-Host "CommunicationEmailServiceName : $CommunicationEmailServiceName"

    $results = @()

    # Resource Group
    $rg = az group show --name $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Resource Group"; Status = ($null -ne $rg); Detail = $ResourceGroup }

    # Container Registry
    $acr = az acr show --name $ContainerRegistryName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Container Registry"; Status = ($null -ne $acr); Detail = $ContainerRegistryName }

    # App Service Plan
    $plan = az appservice plan show --name $AppServicePlanName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "App Service Plan"; Status = ($null -ne $plan); Detail = $AppServicePlanName }

    # API App Service
    $api = az webapp show --name $ApiAppServiceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "API App Service"; Status = ($null -ne $api); Detail = $ApiAppServiceName }

    # Blazor App Service
    $blazor = az webapp show --name $BlazorAppServiceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Blazor App Service"; Status = ($null -ne $blazor); Detail = $BlazorAppServiceName }

    # Storage Account
    $stor = az storage account show --name $StorageAccountName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Storage Account"; Status = ($null -ne $stor); Detail = $StorageAccountName }    

    # Azure Communication Service
    $acs = az resource show `
        --resource-group $ResourceGroup `
        --resource-type Microsoft.Communication/communicationServices `
        --name $CommunicationServiceName `
        --output json 2>$null | ConvertFrom-Json

    $acsValid = $null -ne $acs

    $results += [PSCustomObject]@{
        Name   = "Azure Communication Service"
        Status = $acsValid
        Detail = $CommunicationServiceName
    }

    # Azure Communication Email Service
    $emailService = az communication email show `
        --name $CommunicationEmailServiceName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    $emailServiceValid =
        $null -ne $emailService `
        -and $emailService.provisioningState -eq "Succeeded"

    $results += [PSCustomObject]@{
        Name   = "Communication Email Service"
        Status = $emailServiceValid
        Detail = $CommunicationEmailServiceName
    }

    # Azure Managed Email Domain
    $domain = az communication email domain show `
        --resource-group $ResourceGroup `
        --email-service-name $CommunicationEmailServiceName `
        --domain-name AzureManagedDomain `
        --output json 2>$null | ConvertFrom-Json

    $domainValid =
        $null -ne $domain `
        -and $domain.provisioningState -eq "Succeeded" `
        -and $domain.domainManagement -eq "AzureManaged"

    $results += [PSCustomObject]@{
        Name   = "Azure Managed Email Domain"
        Status = $domainValid
        Detail = "AzureManagedDomain"
    }

    # DoNotReply sender username
    $senderUser = az communication email domain sender-username show `
        --resource-group $ResourceGroup `
        --email-service-name $CommunicationEmailServiceName `
        --domain-name AzureManagedDomain `
        --sender-username DoNotReply `
        --output json 2>$null | ConvertFrom-Json

    $senderValid =
        $null -ne $senderUser `
        -and $senderUser.username -eq "DoNotReply"

    $results += [PSCustomObject]@{
        Name   = "ACS Sender Username"
        Status = $senderValid
        Detail = "DoNotReply"
    }

    # Key Vault
    $kv = az keyvault show --name $KeyVaultName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Key Vault"; Status = ($null -ne $kv); Detail = $KeyVaultName }

    # Application Insights (generic ARM query — no extension required)
    $ai = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Insights/components" `
        --name $ApplicationInsightsName `
        --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Application Insights"; Status = ($null -ne $ai); Detail = $ApplicationInsightsName }

    # Log Analytics
    $la = az monitor log-analytics workspace show --workspace-name $LogAnalyticsWorkspaceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Log Analytics"; Status = ($null -ne $la); Detail = $LogAnalyticsWorkspaceName }

    # SQL Server (already verified in Phase 6, but check existence here too)
    $sqlSrv = az sql server show --name $SqlServerName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "SQL Server"; Status = ($null -ne $sqlSrv); Detail = $SqlServerName }

    # SQL Database
    $sqlDb = az sql db show --name $SqlDatabaseName --server $SqlServerName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "SQL Database"; Status = ($null -ne $sqlDb); Detail = "$SqlServerName/$SqlDatabaseName" }

    # Check all passed
    $allPassed = $true
    foreach ($r in $results) {
        if (-not $r.Status) {
            Write-Fail "$($r.Name) – $($r.Detail)"
            $allPassed = $false
        }
    }

    if (-not $allPassed) {
        exit $EXIT_INFRASTRUCTURE
    }

    return $results
}

# --------------------------------------------------------------------------
# Phase 11 – Verify Azure Monitor Integration (O2)
# --------------------------------------------------------------------------
function Verify-AzureMonitorIntegration {
    param(
        [string]$ResourceGroup,
        [string]$LogAnalyticsWorkspaceName,
        [string]$LogAnalyticsWorkspaceId,
        [string]$ApplicationInsightsName,
        [string]$ApplicationInsightsConnectionString,
        [string]$GrafanaName,
        [string]$GrafanaPrincipalId,
        [string]$ApiAppServiceName,
        [string]$BlazorAppServiceName,
        [string]$SqlServerName,
        [string]$SqlDatabaseName,
        [string]$StorageAccountName,
        [string]$KeyVaultName,
        [string]$CommunicationServiceName
    )

    Write-Step "Phase 11 – Verifying Azure Monitor integration (O2)"

    $subscriptionId = az account show --query id --output tsv
    $results = @()

    # --- Log Analytics workspace exists and is enabled ---
    $la = az monitor log-analytics workspace show `
        --workspace-name $LogAnalyticsWorkspaceName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    $results += [PSCustomObject]@{
        Name   = "Log Analytics workspace"
        Status = ($null -ne $la -and $la.provisioningState -eq "Succeeded")
        Detail = $LogAnalyticsWorkspaceName
    }

    # --- Application Insights: exists, workspace-backed ---
    $ai = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Insights/components" `
        --name $ApplicationInsightsName `
        --output json 2>$null | ConvertFrom-Json

    $aiWorkspaceLinked = $false
    if ($null -ne $ai) {
        $expectedWorkspaceId = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup" +
            "/providers/Microsoft.OperationalInsights/workspaces/$LogAnalyticsWorkspaceName"
        $aiWorkspaceLinked = $ai.properties.WorkspaceResourceId -eq $expectedWorkspaceId
    }

    $results += [PSCustomObject]@{
        Name   = "Application Insights workspace linkage"
        Status = $aiWorkspaceLinked
        Detail = if ($aiWorkspaceLinked) { "-> $LogAnalyticsWorkspaceName" } else { "not linked to expected workspace" }
    }

    $results += [PSCustomObject]@{
        Name   = "Application Insights connection string"
        Status = (-not [string]::IsNullOrWhiteSpace($ApplicationInsightsConnectionString) -and
                  $ApplicationInsightsConnectionString -like "InstrumentationKey=*;IngestionEndpoint=*")
        Detail = "configured (connection-string based, no instrumentation-key-only config)"
    }

    # --- Diagnostic settings per resource ---
    function Test-DiagnosticSetting {
        param([string]$TargetResourceId, [string]$SettingName)

        $ds = az monitor diagnostic-settings show `
            --resource $TargetResourceId `
            --name $SettingName `
            --output json 2>$null | ConvertFrom-Json

        return ($null -ne $ds -and $ds.workspaceId -eq $LogAnalyticsWorkspaceId)
    }

    $rgPath = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup"

    $diagChecks = @(
        @{ Name = "Diagnostics: API App Service";      Resource = "$rgPath/providers/Microsoft.Web/sites/$ApiAppServiceName";                Setting = "atlas-api-diagnostics" },
        @{ Name = "Diagnostics: Blazor App Service";   Resource = "$rgPath/providers/Microsoft.Web/sites/$BlazorAppServiceName";             Setting = "atlas-blazor-diagnostics" },
        @{ Name = "Diagnostics: SQL Database";         Resource = "$rgPath/providers/Microsoft.Sql/servers/$SqlServerName/databases/$SqlDatabaseName"; Setting = "atlas-sql-diagnostics" },
        @{ Name = "Diagnostics: Storage Account (Blob)"; Resource = "$rgPath/providers/Microsoft.Storage/storageAccounts/$StorageAccountName/blobServices/default"; Setting = "atlas-storage-diagnostics" },
        @{ Name = "Diagnostics: Key Vault";            Resource = "$rgPath/providers/Microsoft.KeyVault/vaults/$KeyVaultName";               Setting = "atlas-keyvault-diagnostics" },
        @{ Name = "Diagnostics: Communication Services"; Resource = "$rgPath/providers/Microsoft.Communication/communicationServices/$CommunicationServiceName"; Setting = "atlas-acs-diagnostics" }
    )

    foreach ($check in $diagChecks) {
        $ok = Test-DiagnosticSetting -TargetResourceId $check.Resource -SettingName $check.Setting
        $results += [PSCustomObject]@{
            Name   = $check.Name
            Status = $ok
            Detail = if ($ok) { "$($check.Setting) -> $LogAnalyticsWorkspaceName" } else { "$($check.Setting) missing or wrong destination" }
        }
    }

    # --- Managed Grafana ---
    $grafana = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Dashboard/grafana" `
        --name $GrafanaName `
        --output json 2>$null | ConvertFrom-Json

    $grafanaExists = $null -ne $grafana -and $grafana.properties.provisioningState -eq "Succeeded"

    $results += [PSCustomObject]@{
        Name   = "Managed Grafana provisioned"
        Status = $grafanaExists
        Detail = if ($grafanaExists) { $GrafanaName } else { "$GrafanaName not Succeeded" }
    }

    # --- Grafana managed identity + RBAC ---
    $grafanaIdentityOk = $false
    if ($grafanaExists -and -not [string]::IsNullOrWhiteSpace($GrafanaPrincipalId)) {
        # Monitoring Reader on the resource group grants metric/list access.
        $monitoringReaderDefId = "43d0d8ad-25c7-4714-9337-8ba259a9fe05"
        $assignments = az role assignment list `
            --assignee $GrafanaPrincipalId `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json

        $grafanaIdentityOk = ($assignments | Where-Object {
            $_.roleDefinitionId -like "*$monitoringReaderDefId"
        }) -ne $null

        # Log Analytics Reader on the workspace grants KQL query access.
        $laReaderDefId = "73c42c96-036c-4bb8-9d1a-6a5e0d4b0f7c"
        $laAssignments = az role assignment list `
            --assignee $GrafanaPrincipalId `
            --scope $LogAnalyticsWorkspaceId `
            --output json 2>$null | ConvertFrom-Json

        $grafanaIdentityOk = $grafanaIdentityOk -and (($laAssignments | Where-Object {
            $_.roleDefinitionId -like "*$laReaderDefId"
        }) -ne $null)
    }

    $results += [PSCustomObject]@{
        Name   = "Managed Grafana RBAC"
        Status = $grafanaIdentityOk
        Detail = if ($grafanaIdentityOk) { "Monitoring Reader + Log Analytics Reader assigned" } else { "expected role assignments missing" }
    }

    # Report failures immediately (consistent with Phase 10 behavior).
    $allPassed = $true
    foreach ($r in $results) {
        if (-not $r.Status) {
            Write-Fail "$($r.Name) – $($r.Detail)"
            $allPassed = $false
        } else {
            Write-Pass "$($r.Name) – $($r.Detail)"
        }
    }

    if (-not $allPassed) {
        exit $EXIT_INFRASTRUCTURE
    }

    return $results
}

# --------------------------------------------------------------------------
# --------------------------------------------------------------------------
# Phase 11b - Verify O6 Visualization Resources (Workbook + Grafana dashboard)
# --------------------------------------------------------------------------
function Verify-O6Visualization {
    param(
        [string]$ResourceGroup,
        [string]$OperationsWorkbookName,
        [string]$GrafanaName,
        [string]$GrafanaDashboardName
    )

    Write-Step "Phase 11b - Verifying O6 visualization resources (Workbook + Grafana dashboard)"

    $results = @()

    # --- ATLAS Operations Workbook exists with expected display name ---
    $workbook = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Insights/workbooks" `
        --name $OperationsWorkbookName `
        --output json 2>$null | ConvertFrom-Json

    $workbookOk = $null -ne $workbook -and $workbook.properties.displayName -eq "ATLAS Operations"

    $results += [PSCustomObject]@{
        Name   = "Operations Workbook"
        Status = $workbookOk
        Detail = if ($workbookOk) { "ATLAS Operations" } else { "$OperationsWorkbookName missing or wrong display name" }
    }

    # --- Grafana dashboard provisioned inside the Managed Grafana instance ---
    $dashboard = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Dashboard/grafana/dashboards" `
        --namespace "Microsoft.Dashboard" `
        --parent "grafana/$GrafanaName" `
        --name $GrafanaDashboardName `
        --output json 2>$null | ConvertFrom-Json

    $dashboardOk = $null -ne $dashboard

    $results += [PSCustomObject]@{
        Name   = "Grafana Operations Dashboard"
        Status = $dashboardOk
        Detail = if ($dashboardOk) { "$GrafanaDashboardName in $GrafanaName" } else { "$GrafanaDashboardName missing in $GrafanaName" }
    }

    $allPassed = $true
    foreach ($r in $results) {
        if (-not $r.Status) {
            Write-Fail "$($r.Name) - $($r.Detail)"
            $allPassed = $false
        } else {
            Write-Pass "$($r.Name) - $($r.Detail)"
        }
    }

    if (-not $allPassed) {
        exit $EXIT_INFRASTRUCTURE
    }

    return $results
}

# --------------------------------------------------------------------------
# Phase 11c - Verify O7 Alerting (Action Group + alert rules)
# --------------------------------------------------------------------------
function Verify-O7Alerting {
    param(
        [string]$ResourceGroup,
        [string]$ActionGroupName,
        [array]$ExpectedAlerts
    )

    Write-Step "Phase 11c - Verifying O7 alerting resources"

    $results = @()

    # --- Action Group exists and is enabled ---
    $ag = az monitor action-group show `
        --name $ActionGroupName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    $agOk = $null -ne $ag -and $ag.enabled -eq $true

    $results += [PSCustomObject]@{
        Name   = "Action Group"
        Status = $agOk
        Detail = if ($agOk) { "$ActionGroupName enabled" } else { "$ActionGroupName missing or disabled" }
    }

    # --- Alert rules: exist, enabled, reference the Action Group, correct scope ---
    # Scope expectation model (applied consistently across alert types):
    #   ScopeExact       - rule scope must contain this exact resource ID
    #                      (used for metric alerts targeting a specific App Service)
    #   ScopeContains    - at least one rule scope must contain this substring
    #                      (used for log alerts scoped to App Insights / workspace)
    #   ScopeStartsWith  - every rule scope must start with this prefix
    #                      (used for subscription-scoped activity log alerts)
    foreach ($expected in $ExpectedAlerts) {
        $exists = $false
        $enabled = $false
        $agLinked = $false
        $scopeOk = $false
        $scopeDetail = ""
        $actualScopes = @()

        switch ($expected.Type) {
            "metric" {
                $rule = az monitor metrics alert show `
                    --name $expected.Name `
                    --resource-group $ResourceGroup `
                    --output json 2>$null | ConvertFrom-Json
                if ($null -ne $rule) {
                    $exists = $true
                    $enabled = $rule.enabled -eq $true
                    $agLinked = @($rule.actions | Where-Object { $_.actionGroupId -like "*$ActionGroupName" }).Count -gt 0
                    $actualScopes = @($rule.scopes)
                    $scopeOk = $actualScopes -contains $expected.ScopeExact
                }
            }
            "log" {
                $rule = az monitor scheduled-query show `
                    --name $expected.Name `
                    --resource-group $ResourceGroup `
                    --output json 2>$null | ConvertFrom-Json
                if ($null -ne $rule) {
                    $exists = $true
                    $enabled = $rule.enabled -eq $true
                    $agLinked = @($rule.actions.actionGroups | Where-Object { $_ -like "*$ActionGroupName" }).Count -gt 0
                    $actualScopes = @($rule.scopes)
                    # Substring test (not -like): -like without a trailing
                    # wildcard would require the scope to END with the pattern,
                    # but real scopes end with the resource name.
                    $scopeOk = @($actualScopes | Where-Object { $_.Contains($expected.ScopeContains) }).Count -gt 0
                }
            }
            "activitylog" {
                $rule = az monitor activity-log alert show `
                    --name $expected.Name `
                    --resource-group $ResourceGroup `
                    --output json 2>$null | ConvertFrom-Json
                if ($null -ne $rule) {
                    $exists = $true
                    $enabled = $rule.enabled -eq $true
                    $agLinked = @($rule.actions.actionGroups | Where-Object { $_.actionGroupId -like "*$ActionGroupName" }).Count -gt 0
                    $actualScopes = @($rule.scope)
                    $scopeOk = ($actualScopes.Count -gt 0) -and ($actualScopes | Where-Object { $_.StartsWith($expected.ScopeStartsWith) }).Count -eq $actualScopes.Count
                }
            }
        }

        if (-not $exists) {
            $detail = "missing"
        } elseif (-not $enabled) {
            $detail = "disabled"
        } elseif (-not $agLinked) {
            $detail = "does not reference Action Group $ActionGroupName"
        } elseif (-not $scopeOk) {
            $detail = "wrong scope (expected: $($expected.ScopeExact)$($expected.ScopeContains)$($expected.ScopeStartsWith); actual: $($actualScopes -join ', '))"
        } else {
            $detail = "enabled, scoped correctly, linked to $ActionGroupName"
        }

        $results += [PSCustomObject]@{
            Name   = $expected.Name
            Status = ($exists -and $enabled -and $agLinked -and $scopeOk)
            Detail = $detail
        }
    }

    $allPassed = $true
    foreach ($r in $results) {
        if (-not $r.Status) {
            Write-Fail "$($r.Name) - $($r.Detail)"
            $allPassed = $false
        } else {
            Write-Pass "$($r.Name) - $($r.Detail)"
        }
    }

    if (-not $allPassed) {
        exit $EXIT_INFRASTRUCTURE
    }

    return $results
}
# Phase 12 – Write Summary
# --------------------------------------------------------------------------
function Write-Summary {
    param(
        [PSCustomObject]$DeploymentOutputs,
        [array]$InfrastructureResults,
        [array]$MonitorResults
    )

    Write-Host "`n"
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "ATLAS Infrastructure Bootstrap" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "Deployment" -ForegroundColor Yellow
    Write-Host "  PASS Deployment located" -ForegroundColor Green

    Write-Host ""
    Write-Host "Security" -ForegroundColor Yellow
    Write-Host "  PASS GitHub AcrPush" -ForegroundColor Green
    Write-Host "  PASS API AcrPull" -ForegroundColor Green
    Write-Host "  PASS Blazor AcrPull" -ForegroundColor Green

    Write-Host ""
    Write-Host "SQL" -ForegroundColor Yellow
    Write-Host "  PASS SQL Server" -ForegroundColor Green
    Write-Host "  PASS SQL Administrator" -ForegroundColor Green
    Write-Host "  PASS SQL Firewall" -ForegroundColor Green
    Write-Host "  PASS Database" -ForegroundColor Green

    Write-Host ""
    Write-Host "Infrastructure" -ForegroundColor Yellow
    foreach ($r in $InfrastructureResults) {
        $status = if ($r.Status) { "PASS" } else { "FAIL" }
        $color = if ($r.Status) { "Green" } else { "Red" }
        $name = $r.Name.PadRight(22)
        Write-Host "  $status $name $($r.Detail)" -ForegroundColor $color
    }

    if ($MonitorResults) {
        Write-Host ""
        Write-Host "Azure Monitor (O2)" -ForegroundColor Yellow
        foreach ($r in $MonitorResults) {
            $status = if ($r.Status) { "PASS" } else { "FAIL" }
            $color = if ($r.Status) { "Green" } else { "Red" }
            $name = $r.Name.PadRight(22)
            Write-Host "  $status $name $($r.Detail)" -ForegroundColor $color
        }
    }

    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "Infrastructure ready for GitHub deployment." -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

# ==========================================================================
# Main
# ==========================================================================
Write-Host "ATLAS Infrastructure Bootstrap" -ForegroundColor Cyan
Write-Host "Resource Group : $ResourceGroup"
Write-Host "Deployment     : $DeploymentName"
Write-Host "GitHub Client  : $GitHubClientId"
Write-Host ""

# Prerequisites
Assert-AzureCliInstalled
Assert-AzureLogin

# Phase 1
$outputs = Get-DeploymentOutputs -ResourceGroup $ResourceGroup -DeploymentName $DeploymentName

# Phase 2
$githubSp = Get-GitHubServicePrincipal -ClientId $GitHubClientId

# Phase 3a
Ensure-AcrPushRoleAssignment `
    -PrincipalId $githubSp.id `
    -Scope $outputs.containerRegistryResourceId `
    -PrincipalName "GitHub Actions" `
    -KeyVaultName $outputs.keyVaultName

# Phase 3b
Ensure-DeveloperStorageAccess `
    -StorageAccountResourceId $outputs.storageAccountResourceId    

# Phase 3c
Ensure-AcsPermissions `
    -CommunicationServiceResourceId $outputs.communicationServiceResourceId `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId    

# Phase 4a
Verify-ManagedIdentities `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName

# Phase 4b
Ensure-AppServiceAcrPullConfiguration `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName

# Phase 5
Verify-AcrPermissions `
    -AcrResourceId $outputs.containerRegistryResourceId `
    -GitHubPrincipalId $githubSp.id `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId

# Phase 6
Verify-KeyVaultIntegration `
    -KeyVaultName $outputs.keyVaultName `
    -GitHubPrincipalId $githubSp.id `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId

# Phase 7
Verify-StorageManagedIdentity `
    -StorageAccountResourceId $outputs.storageAccountResourceId `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId

# Phase 8a
Configure-AcsSenderAddress `
    -ResourceGroup $ResourceGroup `
    -EmailServiceName $outputs.communicationEmailServiceName `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName    

# Phase 8b
Verify-AppServiceConfiguration `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName `
    -KeyVaultName $outputs.keyVaultName

# Phase 9
Verify-SqlInfrastructure `
    -SqlServerName $outputs.sqlServerName `
    -SqlDatabaseName $outputs.sqlDatabaseName

# Phase 10
$infraResults = Verify-InfrastructureResources `
    -ContainerRegistryName $outputs.containerRegistryName `
    -AppServicePlanName $outputs.appServicePlanName `
    -ApiAppServiceName $outputs.apiAppServiceName `
    -BlazorAppServiceName $outputs.blazorAppServiceName `
    -StorageAccountName $outputs.storageAccountName `
    -KeyVaultName $outputs.keyVaultName `
    -ApplicationInsightsName $outputs.applicationInsightsName `
    -LogAnalyticsWorkspaceName $outputs.logAnalyticsWorkspaceName `
    -SqlServerName $outputs.sqlServerName `
    -SqlDatabaseName $outputs.sqlDatabaseName `
    -CommunicationServiceName $outputs.communicationServiceName `
    -CommunicationEmailServiceName $outputs.communicationEmailServiceName

# Phase 11 (O2 – Azure Monitor Integration)
$monitorResults = Verify-AzureMonitorIntegration `
    -ResourceGroup $ResourceGroup `
    -LogAnalyticsWorkspaceName $outputs.logAnalyticsWorkspaceName `
    -LogAnalyticsWorkspaceId $outputs.logAnalyticsWorkspaceId `
    -ApplicationInsightsName $outputs.applicationInsightsName `
    -ApplicationInsightsConnectionString $outputs.applicationInsightsConnectionString `
    -GrafanaName $outputs.grafanaName `
    -GrafanaPrincipalId $outputs.grafanaPrincipalId `
    -ApiAppServiceName $outputs.apiAppServiceName `
    -BlazorAppServiceName $outputs.blazorAppServiceName `
    -SqlServerName $outputs.sqlServerName `
    -SqlDatabaseName $outputs.sqlDatabaseName `
    -StorageAccountName $outputs.storageAccountName `
    -KeyVaultName $outputs.keyVaultName `
    -CommunicationServiceName $outputs.communicationServiceName

# Phase 11b - Verify O6 visualization resources (Workbook + Grafana dashboard)
$monitorResults += Verify-O6Visualization `
    -ResourceGroup $ResourceGroup `
    -OperationsWorkbookName $outputs.operationsWorkbookName `
    -GrafanaName $outputs.grafanaName `
    -GrafanaDashboardName $outputs.operationsGrafanaDashboardName

# Phase 11c - Verify O7 alerting resources (Action Group + alert rules)
# Scope expectations use the exact deployment outputs where available so a
# rule targeting the wrong App Service / App Insights is detected precisely.
$o7ExpectedAlerts = @(
    @{ Name = $outputs.apiAvailabilityAlertName;    Type = "metric";      ScopeExact = "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/$($outputs.apiAppServiceName)" },
    @{ Name = $outputs.blazorAvailabilityAlertName; Type = "metric";      ScopeExact = "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/$($outputs.blazorAppServiceName)" },
    @{ Name = $outputs.exceptionSpikeAlertName;     Type = "log";         ScopeContains = "Microsoft.Insights/components" },
    @{ Name = $outputs.emailFailureAlertName;       Type = "log";         ScopeContains = "Microsoft.Insights/components" },
    @{ Name = $outputs.commandLatencyAlertName;     Type = "log";         ScopeContains = "Microsoft.Insights/components" },
    @{ Name = $outputs.serviceHealthAlertName;      Type = "activitylog"; ScopeExact = "/subscriptions/$((az account show --query id --output tsv))" }
)
$alertResults = Verify-O7Alerting `
    -ResourceGroup $ResourceGroup `
    -ActionGroupName $outputs.actionGroupName `
    -ExpectedAlerts $o7ExpectedAlerts

# Phase 12
Write-Summary -DeploymentOutputs $outputs -InfrastructureResults $infraResults -MonitorResults ($monitorResults + $alertResults)

exit $EXIT_SUCCESS
