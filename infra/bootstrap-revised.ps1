<#
.SYNOPSIS
    Bootstraps and validates the supplied ATLAS Azure environment.

.DESCRIPTION
    Performs post-deployment configuration and validation for the supplied ATLAS
    environment defined by the Bicep infrastructure.

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

    The script is intended for the supplied ATLAS environment.

.PARAMETER ResourceGroupName
    Name of the Azure Resource Group containing the supplied ATLAS environment.

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
    [string]$GitHubClientId,

    [Parameter(Mandatory)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment
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

    $requiredOutputs = @(
        'apiAppServiceName'
        'apiAppServiceResourceId'
        'apiHostname'
        'apiPrincipalId'
        'blazorAppServiceName'
        'blazorAppServiceResourceId'
        'blazorHostname'
        'blazorPrincipalId'
    )

    foreach ($outputName in $requiredOutputs) {
        $output = $outputs.$outputName

        Assert-True `
            ($null -ne $output -and -not [string]::IsNullOrWhiteSpace([string]$output.value)) `
            "Required deployment output '$outputName' is missing or empty."
    }

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
        exceptionSpikeAlertName    = $outputs.exceptionSpikeAlertName.value
        emailFailureAlertName      = $outputs.emailFailureAlertName.value
        commandLatencyAlertName    = $outputs.commandLatencyAlertName.value
        serviceHealthAlertName     = $outputs.serviceHealthAlertName.value
        grafanaName                = $outputs.grafanaName.value
        grafanaEndpoint            = $outputs.grafanaEndpoint.value
        grafanaPrincipalId         = $outputs.grafanaPrincipalId.value
        grafanaResourceId          = $outputs.grafanaResourceId.value
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
# Phase 2 – Configure GitHub AcrPush
# --------------------------------------------------------------------------
function Ensure-AcrPushRoleAssignment {
    param(
        [string]$PrincipalId,
        [string]$Scope,
        [string]$PrincipalName,
        [string]$KeyVaultName
    )

    Write-Step "Phase 2 – Configuring GitHub Actions access"

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
# Phase 2 – Configure Developer Storage Access
# --------------------------------------------------------------------------
function Ensure-DeveloperStorageAccess {
    param(
        [string]$StorageAccountResourceId
    )

    Write-Step "Phase 2 – Configuring developer Blob Storage access"

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
# Phase 2 – Configure Azure Communication Services permissions
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

    Write-Step "Phase 2 – Configuring Azure Communication Services permissions"

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
# Phase 3 – Verify Managed Identities
# --------------------------------------------------------------------------
function Verify-ManagedIdentities {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName
    )

    Write-Step "Phase 3 – Verifying App Service managed identities"

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
# Phase 3 – Configure App Service ACR Pull Authentication
# --------------------------------------------------------------------------
function Ensure-AppServiceAcrPullConfiguration {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName
    )

    Write-Step "Phase 3 – Configuring App Service ACR pull authentication"

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
# Phase 4 – Configure and Verify ACR Permissions
# --------------------------------------------------------------------------
function Verify-AcrPermissions {
    param(
        [string]$AcrResourceId,
        [string]$GitHubPrincipalId,
        [string]$ApiPrincipalId,
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 4 – Verifying ACR permissions"

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
# Phase 4 – Verify Key Vault Integration
# --------------------------------------------------------------------------
function Verify-KeyVaultIntegration {
    param(
        [string]$KeyVaultName,
        [string]$GitHubPrincipalId,
        [string]$ApiPrincipalId,
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 4 – Verifying Key Vault integration"

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
# Phase 4 – Verify Storage Managed Identity Integration
# --------------------------------------------------------------------------
function Verify-StorageManagedIdentity {
    param(
        [string]$StorageAccountResourceId,
        [string]$ApiPrincipalId,
        [string]$BlazorPrincipalId
    )

    Write-Step "Phase 4 – Verifying Storage Managed Identity integration"

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
# Phase 3 – Configure Azure Communication Services sender address
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

    Write-Step "Phase 3 – Configuring Azure Communication Services sender address"

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

        $maxAttempts = 5
        $retryDelaySeconds = 5

        for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {

            Write-Host "Configuring sender address on $AppName (attempt $attempt/$maxAttempts)..."

            az webapp config appsettings set `
                --resource-group $ResourceGroup `
                --name $AppName `
                --settings "Email__Acs__SenderAddress=$senderAddress" `
                --only-show-errors | Out-Null

            if ($LASTEXITCODE -ne 0) {
                if ($attempt -eq $maxAttempts) {
                    Write-Fail "$AppName failed to update Email__Acs__SenderAddress."
                    exit $EXIT_INFRASTRUCTURE
                }

                Start-Sleep -Seconds $retryDelaySeconds
                continue
            }

            # Read the value back from Azure and verify that the setting
            # is actually present before continuing.
            $verifiedSender = az webapp config appsettings list `
                --resource-group $ResourceGroup `
                --name $AppName `
                --query "[?name=='Email__Acs__SenderAddress'].value | [0]" `
                -o tsv `
                --only-show-errors

            if ($LASTEXITCODE -eq 0 -and $verifiedSender -eq $senderAddress) {
                Write-Pass "$AppName sender address configured."
                break
            }

            if ($attempt -eq $maxAttempts) {
                Write-Fail "$AppName sender address could not be verified after $maxAttempts attempts."
                exit $EXIT_INFRASTRUCTURE
            }

            Start-Sleep -Seconds $retryDelaySeconds
        }

        # Restart only after the setting has definitely been confirmed.
        az webapp restart `
            --resource-group $ResourceGroup `
            --name $AppName `
            --only-show-errors | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Fail "$AppName restart failed."
            exit $EXIT_INFRASTRUCTURE
        }

        Write-Pass "$AppName restarted."

        # Verify once more after restart.
        for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {

            $verifiedSender = az webapp config appsettings list `
                --resource-group $ResourceGroup `
                --name $AppName `
                --query "[?name=='Email__Acs__SenderAddress'].value | [0]" `
                -o tsv `
                --only-show-errors

            if ($LASTEXITCODE -eq 0 -and $verifiedSender -eq $senderAddress) {
                Write-Pass "$AppName sender address verified after restart."
                return
            }

            if ($attempt -lt $maxAttempts) {
                Start-Sleep -Seconds $retryDelaySeconds
            }
        }

        Write-Fail "$AppName sender address verification failed after restart."
        exit $EXIT_INFRASTRUCTURE
    }

    #
    # Configure both App Services
    #
    Update-AppServiceSenderAddress -AppName $ApiAppName
    Update-AppServiceSenderAddress -AppName $BlazorAppName
}

# --------------------------------------------------------------------------
# Phase 3 – Verify App Service Configuration
# --------------------------------------------------------------------------
function Verify-AppServiceConfiguration {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName,
        [string]$KeyVaultName
    )

    Write-Step "Phase 3 – Verifying App Service configuration"

    foreach ($appName in @($ApiAppName, $BlazorAppName)) {

        $settingsJson = az webapp config appsettings list `
            --name $appName `
            --resource-group $ResourceGroup `
            --output json `
            --only-show-errors

        Assert-True ($LASTEXITCODE -eq 0) `
            "Unable to read App Service settings for '$appName'."

        $settings = $settingsJson | ConvertFrom-Json

        Assert-True ($null -ne $settings) `
            "Unable to parse App Service settings for '$appName'."

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
# Phase 4 – Verify SQL
# --------------------------------------------------------------------------
function Verify-SqlInfrastructure {
    param(
        [string]$SqlServerName,
        [string]$SqlDatabaseName
    )

    Write-Step "Phase 4 – Verifying SQL infrastructure"

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
# Phase 4 – Verify Infrastructure Resources
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

    Write-Step "Phase 4 – Verifying core infrastructure resources" 

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
        else {
            Write-Pass "$($r.Name) – $($r.Detail)"
        }
    }

    if (-not $allPassed) {
        exit $EXIT_INFRASTRUCTURE
    }

    return $results
}

# --------------------------------------------------------------------------
# Phase 5 – Verify Azure Monitor Integration (O2)
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
        [string]$CommunicationServiceName,
        [string]$CommunicationServiceResourceId,
        [string]$Environment
    )

    Write-Step "Phase 5 – Verifying Azure Monitor integration (O2)"

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
        @{ Name = "Diagnostics: Key Vault";            Resource = "$rgPath/providers/Microsoft.KeyVault/vaults/$KeyVaultName";               Setting = "atlas-keyvault-diagnostics" }     
    )

    foreach ($check in $diagChecks) {
        $ok = Test-DiagnosticSetting -TargetResourceId $check.Resource -SettingName $check.Setting
        $results += [PSCustomObject]@{
            Name   = $check.Name
            Status = $ok
            Detail = if ($ok) { "$($check.Setting) -> $LogAnalyticsWorkspaceName" } else { "$($check.Setting) missing or wrong destination" }
        }
    }

   $acsDiagnosticSetting = az monitor diagnostic-settings show `
        --resource $outputs.communicationServiceResourceId `
        --name 'Email_Logs' `
        --output json | ConvertFrom-Json

    if (-not $acsDiagnosticSetting) {
        Write-Fail "ACS diagnostic setting 'Email_Logs' not found."
        exit $EXIT_INFRASTRUCTURE
    }

    $enabledCategories = @(
        $acsDiagnosticSetting.logs |
            Where-Object { $_.enabled -eq $true } |
            ForEach-Object { $_.category }
    )

    $requiredCategories = @(
        'EmailSendMailOperational'
        'EmailStatusUpdateOperational'
    )

    $missingCategories = @(
        $requiredCategories |
            Where-Object { $_ -notin $enabledCategories }
    )

    if ($missingCategories.Count -gt 0) {
        Write-Fail "ACS diagnostic setting 'Email_Logs' is missing required categories: $($missingCategories -join ', ')"
        exit $EXIT_INFRASTRUCTURE
    }

    Write-Pass "ACS diagnostic setting 'Email_Logs' verified with required email categories"

    if ($Environment -ne 'dev') {
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
            $laReaderDefId = "73c42c96-874c-492b-b04d-ab87d138a893"
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
# Phase 6 - Provision & Verify O6 Visualization Resources
#              (Workbook verified via ARM; Grafana dashboard provisioned and
#               verified via the Managed Grafana dashboard API)
#
# The Microsoft.Dashboard/grafana/dashboards ARM sub-resource is NOT a valid
# registered resource type (preflight fails with ResourceTypeRegistrationNotFound),
# so the dashboard is provisioned here instead of in Bicep. Authentication uses
# an Azure AD access token obtained dynamically from the authenticated Azure CLI
# session - no Grafana API keys, service accounts, or static credentials.
# The operation is an idempotent create/update: re-running bootstrap updates
# the same dashboard rather than creating duplicates.
# --------------------------------------------------------------------------
function Invoke-GrafanaDashboardProvisioning {
    param(
        [string]$GrafanaEndpoint,
        [string]$WorkspaceId,
        [string]$DashboardJsonPath
    )

    # --- Grafana instance must be reachable ---
    if ([string]::IsNullOrWhiteSpace($GrafanaEndpoint)) {
        Write-Fail "Grafana instance not found (no endpoint output from deployment)."
        exit $EXIT_INFRASTRUCTURE
    }

    $baseUrl = $GrafanaEndpoint.TrimEnd('/')

    # --- Obtain Azure AD token for the Managed Grafana data-plane API ---
    # Token is held only in memory; never printed or persisted.
    $token = az account get-access-token `
        --resource https://dashboard.azure.com `
        --query accessToken --output tsv

    if ([string]::IsNullOrWhiteSpace($token)) {
        Write-Fail "Grafana authentication failed (could not obtain Azure AD access token)."
        exit $EXIT_INFRASTRUCTURE
    }

    # --- Build payload from the canonical dashboard JSON ---
    Assert-True (Test-Path $DashboardJsonPath) `
        "Dashboard definition not found at '$DashboardJsonPath'."

    $raw = Get-Content $DashboardJsonPath -Raw

    # Resolve deployment placeholders (same tokens previously replaced by Bicep).
    $resolved = $raw.Replace('__WORKSPACE_ID__', $WorkspaceId)

    # Fail fast on any unresolved deployment placeholder rather than
    # provisioning a broken dashboard.
    if ($resolved -match '__[A-Z_]+__') {
        Write-Fail "Unresolved deployment placeholder(s) remain in dashboard definition: $($Matches[0])."
        exit $EXIT_INFRASTRUCTURE
    }

    $dashboardModel = $resolved | ConvertFrom-Json

    # Grafana's dashboard API wraps the model with metadata. UID comes from the
    # JSON so re-running updates the same dashboard (idempotent upsert).
    $payload = @{
        dashboard = $dashboardModel
        message   = 'Provisioned by ATLAS bootstrap.ps1'
        overwrite = $true
    } | ConvertTo-Json -Depth 100

    # --- Idempotent create/update via the Grafana HTTP API ---
    try {
        $response = Invoke-RestMethod `
            -Method Post `
            -Uri "$baseUrl/api/dashboards/db" `
            -Headers @{ Authorization = "Bearer $token" } `
            -ContentType 'application/json' `
            -Body $payload `
            -ErrorAction Stop
    }
    catch {
        $status = $null
        if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode }
        Write-Fail "Grafana dashboard provisioning failed (HTTP status: $status)."
        exit $EXIT_INFRASTRUCTURE
    }

    if (-not $response -or $response.status -ne 'success' -or [string]::IsNullOrWhiteSpace($response.uid)) {
        Write-Fail "Grafana dashboard provisioning failed (unexpected API response)."
        exit $EXIT_INFRASTRUCTURE
    }

    return $response.uid
}

function Provision-O6GrafanaDashboard {
    param(
        [string]$GrafanaName,
        [string]$GrafanaEndpoint,
        [string]$GrafanaResourceId,
        [string]$LogAnalyticsWorkspaceId,
        [string]$DashboardJsonPath
    )

    Write-Step "Phase 6 – Provisioning Grafana Operations Dashboard"

    $grafanaEditorRoleId = 'a79a5197-3a5c-4973-a920-486035ffd60f'
    $currentUser = az ad signed-in-user show --output json 2>$null | ConvertFrom-Json
    if ($null -eq $currentUser -or [string]::IsNullOrWhiteSpace($currentUser.id)) {
        Write-Fail "Grafana bootstrap identity RBAC: could not determine the current Azure CLI identity."
        exit $EXIT_INFRASTRUCTURE
    }

    $editorAssignments = az role assignment list `
        --assignee-object-id $currentUser.id `
        --scope $GrafanaResourceId `
        --role $grafanaEditorRoleId `
        --output json 2>$null | ConvertFrom-Json

    if (@($editorAssignments).Count -eq 0) {
        Write-Fail "Grafana bootstrap identity RBAC: current Azure CLI user does not have Grafana Editor on $GrafanaName."
        exit $EXIT_INFRASTRUCTURE
    }

    Write-Pass "Grafana bootstrap identity RBAC - current Azure CLI user has Grafana Editor on $GrafanaName"

    $uid = Invoke-GrafanaDashboardProvisioning `
        -GrafanaEndpoint $GrafanaEndpoint `
        -WorkspaceId $LogAnalyticsWorkspaceId `
        -DashboardJsonPath $DashboardJsonPath

    Write-Pass "Grafana Operations Dashboard provisioned - $uid"
    return $uid
}

function Verify-O6Visualization {
    param(
        [string]$ResourceGroup,
        [string]$OperationsWorkbookName,     
        [string]$ApplicationInsightsName,   
        [string]$GrafanaName,
        [string]$GrafanaEndpoint,
        [string]$GrafanaResourceId,
        [string]$Environment,
        [string]$ExpectedDashboardUid = 'atlas-operations'        
    )

    Write-Step "Phase 6 – Verifying O6 visualization resources"

    $results = @()

    # --- Operations Workbook: ALWAYS verify ---
    $workbook = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Insights/workbooks" `
        --name $OperationsWorkbookName `
        --output json 2>$null | ConvertFrom-Json

    $subscriptionId = az account show --query id --output tsv 2>$null

    $expectedApplicationInsightsResourceId =
        "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Insights/components/$ApplicationInsightsName"

    $workbookOk =
        $null -ne $workbook -and
        $workbook.properties.displayName -eq "ATLAS Operations" -and
        $workbook.properties.sourceId -eq $expectedApplicationInsightsResourceId

    $results += [PSCustomObject]@{
        Name   = "Operations Workbook"
        Status = $workbookOk
        Detail = if ($workbookOk) {
            "ATLAS Operations -> $expectedApplicationInsightsResourceId"
        }
        else {
            if ($null -eq $workbook) {
                "$OperationsWorkbookName missing"
            }
            elseif ($workbook.properties.displayName -ne "ATLAS Operations") {
                "wrong display name"
            }
            else {
                "wrong or missing sourceId: '$($workbook.properties.sourceId)'"
            }
        }
    }    

    # --- Managed Grafana: test/prod only ---
    if ($Environment -ne 'dev') {
        $grafanaExists = az resource show `
            --resource-group $ResourceGroup `
            --resource-type "Microsoft.Dashboard/grafana" `
            --name $GrafanaName `
            --output json 2>$null | ConvertFrom-Json

        $grafanaOk = $null -ne $grafanaExists

        $results += [PSCustomObject]@{
            Name   = "Managed Grafana"
            Status = $grafanaOk
            Detail = if ($grafanaOk) { $GrafanaName } else { "$GrafanaName missing" }
        }

        $token = az account get-access-token `
            --resource https://dashboard.azure.com `
            --query accessToken --output tsv

        if ([string]::IsNullOrWhiteSpace($token)) {
            Write-Fail "Grafana authentication failed during verification."
            exit $EXIT_INFRASTRUCTURE
        }

        try {
            $fetched = Invoke-RestMethod `
                -Method Get `
                -Uri "$($GrafanaEndpoint.TrimEnd('/'))/api/dashboards/uid/$ExpectedDashboardUid" `
                -Headers @{ Authorization = "Bearer $token" } `
                -ErrorAction Stop
        }
        catch {
            Write-Fail "Grafana dashboard verification failed (dashboard could not be retrieved)."
            exit $EXIT_INFRASTRUCTURE
        }

        $dashboardOk =
            ($null -ne $fetched) -and
            ($fetched.dashboard.uid -eq $ExpectedDashboardUid) -and
            (@($fetched.dashboard.panels).Count -gt 0)

        $results += [PSCustomObject]@{
            Name   = "Grafana Operations Dashboard"
            Status = $dashboardOk
            Detail = if ($dashboardOk) {
                "$ExpectedDashboardUid provisioned and verified at $($GrafanaEndpoint.TrimEnd('/'))"
            } else {
                "verification failed"
            }
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

# --------------------------------------------------------------------------
# Phase 7 – Provision availability metric alerts
# --------------------------------------------------------------------------
function Ensure-AvailabilityMetricAlerts {
    param(
        [string]$ResourceGroup,
        [string]$Environment,
        [string]$ActionGroupName,
        [string]$ApiAppServiceName,
        [string]$BlazorAppServiceName
    )

    Write-Step "Phase 7 – Provisioning availability metric alerts"

    $subscriptionId = az account show --query id --output tsv 2>$null

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($subscriptionId)) {
        Write-Fail "Unable to resolve Azure subscription ID."
        exit $EXIT_INFRASTRUCTURE
    }

    $actionGroup = az monitor action-group show `
        --name $ActionGroupName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    if ($null -eq $actionGroup) {
        Write-Fail "Action Group '$ActionGroupName' not found."
        exit $EXIT_INFRASTRUCTURE
    }

    $actionGroupId = $actionGroup.id

    $alerts = @(
        @{
            Name        = "atlas-$Environment-api-availability"
            AppService  = $ApiAppServiceName
            Description = "ATLAS API health check (/health/ready) has been failing for 15+ minutes. The API is not serving healthy responses. Investigate via ATLAS Operations Portal and Application Insights availability/results."
        },
        @{
            Name        = "atlas-$Environment-blazor-availability"
            AppService  = $BlazorAppServiceName
            Description = "ATLAS Blazor app health check (/health/ready) has been failing for 15+ minutes. The application is not serving healthy responses. Investigate via ATLAS Operations Portal and Application Insights availability/results."
        }
    )

    foreach ($alert in $alerts) {
        $scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/$($alert.AppService)"

        Write-Host "  Checking availability alert: $($alert.Name)..."

        $existing = az monitor metrics alert show `
            --name $alert.Name `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json

        if ($null -ne $existing) {
            Write-Pass "$($alert.Name) already exists"
            continue
        }

        Write-Host "  Creating availability alert: $($alert.Name)..."

        $created = $false

        # HealthCheckStatus can take a short time to become available
        # to the Azure Monitor metric-alert service after App Service
        # provisioning. Retry only that specific transient condition.
        for ($attempt = 1; $attempt -le 12; $attempt++) {
            $errorOutput = & az monitor metrics alert create `
                --name $alert.Name `
                --resource-group $ResourceGroup `
                --scopes $scope `
                --condition "avg HealthCheckStatus < 1" `
                --window-size 15m `
                --evaluation-frequency 5m `
                --severity 1 `
                --auto-mitigate true `
                --action $actionGroupId `
                --description $alert.Description `
                --output none 2>&1

            if ($LASTEXITCODE -eq 0) {
                $created = $true
                break
            }

            $errorText = ($errorOutput -join "`n")

            if ($errorText -notmatch "Couldn't find a metric named HealthCheckStatus") {
                Write-Fail "Failed to create $($alert.Name): $errorText"
                exit $EXIT_INFRASTRUCTURE
            }

            if ($attempt -lt 12) {
                Write-Host "  HealthCheckStatus not yet available; retrying in 10 seconds (attempt $attempt/12)..."
                Start-Sleep -Seconds 10
            }
        }

        if (-not $created) {
            Write-Fail "Timed out waiting for HealthCheckStatus for $($alert.Name)."
            exit $EXIT_INFRASTRUCTURE
        }

        Write-Pass "$($alert.Name) created"
    }
}

# --------------------------------------------------------------------------
# Phase 7 – Verify O7 Alerting (Action Group + alert rules)
# --------------------------------------------------------------------------
function Verify-O7Alerting {
    param(
        [string]$ResourceGroup,
        [string]$ActionGroupName,
        [array]$ExpectedAlerts
    )

    Write-Step "Phase 7 – Verifying O7 alerting resources"

    $results = @()

    # --- Action Group exists and is enabled ---
    Write-Host "  Checking action group: $ActionGroupName..."

    $ag = az monitor action-group show `
        --name $ActionGroupName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    Write-Host "  Finished checking action group: $ActionGroupName"

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
                Write-Host "  Checking metric alert: $($expected.Name)..."

                $rule = az monitor metrics alert show `
                    --name $expected.Name `
                    --resource-group $ResourceGroup `
                    --output json | ConvertFrom-Json

                Write-Host "  Finished metric alert: $($expected.Name)"

                if ($null -ne $rule) {
                    $exists = $true
                    $enabled = $rule.enabled -eq $true
                    $agLinked = @($rule.actions | Where-Object { $_.actionGroupId -like "*$ActionGroupName" }).Count -gt 0
                    $actualScopes = @($rule.scopes)
                    $scopeOk = $actualScopes -contains $expected.ScopeExact
                }
            }
            "log" {
                Write-Host "  Checking log alert: $($expected.Name)..."

                $rule = az resource show `
                    --resource-group $ResourceGroup `
                    --name $expected.Name `
                    --resource-type "Microsoft.Insights/scheduledQueryRules" `
                    --output json 2>$null | ConvertFrom-Json

                Write-Host "  Finished log alert: $($expected.Name)"

                if ($null -ne $rule) {
                    $exists = $true
                    $enabled = $rule.properties.enabled -eq $true
                    $agLinked = @(
                        $rule.properties.actions.actionGroups |
                            Where-Object { $_ -like "*$ActionGroupName" }
                    ).Count -gt 0
                    $actualScopes = @($rule.properties.scopes)

                    $scopeOk = @(
                        $actualScopes |
                            Where-Object { $_.Contains($expected.ScopeContains) }
                    ).Count -gt 0
                }
            }
           "activitylog" {
                Write-Host "  Checking activity log alert: $($expected.Name)..."

                $rule = az resource show `
                    --resource-group $ResourceGroup `
                    --name $expected.Name `
                    --resource-type "Microsoft.Insights/activityLogAlerts" `
                    --output json 2>$null | ConvertFrom-Json

                Write-Host "  Finished activity log alert: $($expected.Name)"

                if ($null -ne $rule) {
                    $exists = $true
                    $enabled = $rule.properties.enabled -eq $true
                    $agLinked = @(
                        $rule.properties.actions.actionGroups |
                            Where-Object { $_.actionGroupId -like "*$ActionGroupName" }
                    ).Count -gt 0
                    $actualScopes = @($rule.properties.scopes)

                    $scopeOk =
                        ($actualScopes.Count -gt 0) -and
                        (@(
                            $actualScopes |
                                Where-Object {
                                    $_.StartsWith($expected.ScopeStartsWith)
                                }
                        ).Count -eq $actualScopes.Count)
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
# Phase 8 – Write Summary
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

# Phase 1 – Deployment
$outputs = Get-DeploymentOutputs -ResourceGroup $ResourceGroup -DeploymentName $DeploymentName

# Phase 2 – Access & RBAC
$githubSp = Get-GitHubServicePrincipal -ClientId $GitHubClientId
Ensure-AcrPushRoleAssignment `
    -PrincipalId $githubSp.id `
    -Scope $outputs.containerRegistryResourceId `
    -PrincipalName "GitHub Actions" `
    -KeyVaultName $outputs.keyVaultName
Ensure-DeveloperStorageAccess `
    -StorageAccountResourceId $outputs.storageAccountResourceId
Ensure-AcsPermissions `
    -CommunicationServiceResourceId $outputs.communicationServiceResourceId `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId

# Phase 3 – App Services
Verify-ManagedIdentities `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName
Ensure-AppServiceAcrPullConfiguration `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName
Configure-AcsSenderAddress `
    -ResourceGroup $ResourceGroup `
    -EmailServiceName $outputs.communicationEmailServiceName `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName
Verify-AppServiceConfiguration `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName `
    -KeyVaultName $outputs.keyVaultName

# Phase 4 – Core Infrastructure
Verify-AcrPermissions `
    -AcrResourceId $outputs.containerRegistryResourceId `
    -GitHubPrincipalId $githubSp.id `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId
Verify-KeyVaultIntegration `
    -KeyVaultName $outputs.keyVaultName `
    -GitHubPrincipalId $githubSp.id `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId
Verify-StorageManagedIdentity `
    -StorageAccountResourceId $outputs.storageAccountResourceId `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId
Verify-SqlInfrastructure `
    -SqlServerName $outputs.sqlServerName `
    -SqlDatabaseName $outputs.sqlDatabaseName
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

# Phase 5 – Azure Monitor
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
    -CommunicationServiceName $outputs.communicationServiceName `
    -CommunicationServiceResourceId $outputs.communicationServiceResourceId `
    -Environment $Environment

# Phase 6 – Visualization
$dashboardJsonPath = Join-Path $PSScriptRoot 'telemetry/atlas-operations.grafana-dashboard.json'

if ($Environment -ne 'dev') {
    Provision-O6GrafanaDashboard `
        -GrafanaName $outputs.grafanaName `
        -GrafanaEndpoint $outputs.grafanaEndpoint `
        -GrafanaResourceId $outputs.grafanaResourceId `
        -LogAnalyticsWorkspaceId $outputs.logAnalyticsWorkspaceId `
        -DashboardJsonPath $dashboardJsonPath | Out-Null
}

$monitorResults += Verify-O6Visualization `
    -ResourceGroup $ResourceGroup `
    -OperationsWorkbookName $outputs.operationsWorkbookName `
    -ApplicationInsightsName $outputs.applicationInsightsName `
    -GrafanaName $outputs.grafanaName `
    -GrafanaEndpoint $outputs.grafanaEndpoint `
    -GrafanaResourceId $outputs.grafanaResourceId `
    -Environment $Environment

# Phase 7 – Alerting
Ensure-AvailabilityMetricAlerts `
    -ResourceGroup $ResourceGroup `
    -Environment $Environment `
    -ActionGroupName $outputs.actionGroupName `
    -ApiAppServiceName $outputs.apiAppServiceName `
    -BlazorAppServiceName $outputs.blazorAppServiceName

$o7ExpectedAlerts = @(
    @{ Name = "atlas-$Environment-api-availability";    Type = "metric";      ScopeExact = "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/$($outputs.apiAppServiceName)" },
    @{ Name = "atlas-$Environment-blazor-availability"; Type = "metric";      ScopeExact = "/subscriptions/$((az account show --query id --output tsv))/resourceGroups/$ResourceGroup/providers/Microsoft.Web/sites/$($outputs.blazorAppServiceName)" },
    @{ Name = $outputs.exceptionSpikeAlertName;          Type = "log";         ScopeContains = "Microsoft.Insights/components" },
    @{ Name = $outputs.emailFailureAlertName;            Type = "log";         ScopeContains = "Microsoft.Insights/components" },
    @{ Name = $outputs.commandLatencyAlertName;          Type = "log";         ScopeContains = "Microsoft.Insights/components" },
    @{ Name = $outputs.serviceHealthAlertName;           Type = "activitylog"; ScopeStartsWith = "/subscriptions/$((az account show --query id --output tsv))" }
)
$alertResults = Verify-O7Alerting `
    -ResourceGroup $ResourceGroup `
    -ActionGroupName $outputs.actionGroupName `
    -ExpectedAlerts $o7ExpectedAlerts

# Phase 8 – Summary
Write-Summary -DeploymentOutputs $outputs -InfrastructureResults $infraResults -MonitorResults ($monitorResults + $alertResults)

exit $EXIT_SUCCESS
