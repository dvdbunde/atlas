<#
.SYNOPSIS
    Bootstraps the ATLAS Azure infrastructure after Bicep deployment.

.DESCRIPTION
    Automates post-deployment configuration and validation steps required
    after a successful 'az deployment group create' of the ATLAS Bicep
    infrastructure. This script does NOT deploy Azure resources — that
    remains the responsibility of main.bicep.

    The script consumes deployment outputs from main.bicep as the single
    source of truth for Azure resource names. It does not duplicate names.

    Phases:
      1. Load deployment outputs from the ARM deployment
      2. Resolve the GitHub Actions Service Principal
      3. Configure AcrPush for the GitHub Service Principal
      4. Verify App Service Managed Identities
      5. Verify AcrPull permissions for both App Services
      6. Verify SQL Server, Database, Administrator, and Firewall
      7. Verify all expected Azure resources exist
      8. Print deployment summary

.PARAMETER ResourceGroup
    The Azure Resource Group name where ATLAS is deployed.

.PARAMETER DeploymentName
    The name of the Bicep deployment to retrieve outputs from.

.PARAMETER GitHubClientId
    The Application (Client) ID of the ATLAS GitHub Actions
    Microsoft Entra App Registration.

.EXAMPLE
    .\bootstrap.ps1 `
        -ResourceGroup atlas-dev-rg `
        -DeploymentName main `
        -GitHubClientId "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"

.NOTES
    Requires: PowerShell 7, Azure CLI, logged into Azure (az login)
    Idempotent: Safe to run multiple times.
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
$EXIT_RBAC              = 4
$EXIT_SQL               = 5
$EXIT_INFRASTRUCTURE    = 6
$EXIT_PREREQUISITES     = 7

# --------------------------------------------------------------------------
# Constants
# --------------------------------------------------------------------------
$AcrPushRoleId = '8311e382-0749-4cb8-b61a-304f252e45ec'
$AcrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
$BlobContainerName = 'permit-documents'
$StorageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

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
# Phase 3 – Configure GitHub AcrPush
# --------------------------------------------------------------------------
function Ensure-AcrPushRoleAssignment {
    param(
        [string]$PrincipalId,
        [string]$Scope,
        [string]$PrincipalName
    )

    Write-Step "Phase 3 – Configuring AcrPush for $PrincipalName"

    $existing = az role assignment list `
        --assignee $PrincipalId `
        --scope $Scope `
        --role $AcrPushRoleId `
        --output json 2>$null | ConvertFrom-Json

    if ($existing) {
        Write-Pass "AcrPush already assigned to $PrincipalName"
        return
    }

    az role assignment create `
        --assignee-object-id $PrincipalId `
        --assignee-principal-type ServicePrincipal `
        --role $AcrPushRoleId `
        --scope $Scope `
        --output none 2>$null

    # Immediate verification
    $verify = az role assignment list `
        --assignee $PrincipalId `
        --scope $Scope `
        --role $AcrPushRoleId `
        --output json 2>$null | ConvertFrom-Json

    Assert-True ($verify -ne $null) "AcrPush role assignment for $PrincipalName could not be verified."
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

    if ($assignment.Count -gt 0) {
        Write-Pass "Developer already has Storage Blob Data Contributor"
        return
    }

    az role assignment create `
        --assignee $currentUser.id `
        --role $StorageBlobDataContributorRoleId `
        --scope $StorageAccountResourceId `
        --output none

    Write-Pass "Developer granted Storage Blob Data Contributor"
}

# --------------------------------------------------------------------------
# Phase 4 – Verify Managed Identities
# --------------------------------------------------------------------------
function Verify-ManagedIdentities {
    param(
        [string]$ApiAppName,
        [string]$BlazorAppName
    )

    Write-Step "Phase 4 – Verifying Managed Identities"

    foreach ($appName in @($ApiAppName, $BlazorAppName)) {
        $app = az webapp show `
            --name $appName `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json

        Assert-True ($app -ne $null) "App Service '$appName' not found."

        $identity = $app.identity
        Assert-True ($identity -ne $null) "App Service '$appName' has no Managed Identity."
        Assert-True ($identity.type -eq 'SystemAssigned') "App Service '$appName' does not use SystemAssigned identity."

        Write-Pass "$appName has SystemAssigned Managed Identity (Principal ID: $($identity.principalId))"
    }
}

# --------------------------------------------------------------------------
# Phase 5 – Verify ACR Permissions
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
    Assert-True ($ghPush -ne $null) "GitHub Service Principal missing AcrPush on ACR."
    Write-Pass "GitHub AcrPush"

    # API AcrPull
    $apiPull = az role assignment list `
        --assignee $ApiPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPullRoleId `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($apiPull -ne $null) "API App Service missing AcrPull on ACR."
    Write-Pass "API AcrPull"

    # Blazor AcrPull
    $blazorPull = az role assignment list `
        --assignee $BlazorPrincipalId `
        --scope $AcrResourceId `
        --role $AcrPullRoleId `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($blazorPull -ne $null) "Blazor App Service missing AcrPull on ACR."
    Write-Pass "Blazor AcrPull"
}

# --------------------------------------------------------------------------
# Phase 6 – Verify SQL
# --------------------------------------------------------------------------
function Verify-SqlInfrastructure {
    param(
        [string]$SqlServerName,
        [string]$SqlDatabaseName
    )

    Write-Step "Phase 6 – Verifying SQL infrastructure"

    # SQL Server exists
    $server = az sql server show `
        --name $SqlServerName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json
    Assert-True ($server -ne $null) "SQL Server '$SqlServerName' not found."
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
    Assert-True ($db -ne $null) "SQL Database '$SqlDatabaseName' not found."
    Write-Pass "Database"

    # SQL Firewall – AllowAzureServices
    $fwRules = az sql server firewall-rule list `
        --server $SqlServerName `
        --resource-group $ResourceGroup `
        --output json 2>$null | ConvertFrom-Json

    $allowAzure = $fwRules | Where-Object { $_.name -eq 'AllowAzureServices' -or $_.startIpAddress -eq '0.0.0.0' -and $_.endIpAddress -eq '0.0.0.0' }
    Assert-True ($allowAzure -ne $null) "SQL Firewall rule 'AllowAzureServices' (0.0.0.0 – 0.0.0.0) not found."
    Write-Pass "SQL Firewall"
}

# --------------------------------------------------------------------------
# Phase 7 – Verify Infrastructure Resources
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
        [string]$SqlDatabaseName
    )

    Write-Step "Phase 7 – Verifying infrastructure resources"

    $results = @()

    # Resource Group
    $rg = az group show --name $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Resource Group"; Status = ($rg -ne $null); Detail = $ResourceGroup }

    # Container Registry
    $acr = az acr show --name $ContainerRegistryName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Container Registry"; Status = ($acr -ne $null); Detail = $ContainerRegistryName }

    # App Service Plan
    $plan = az appservice plan show --name $AppServicePlanName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "App Service Plan"; Status = ($plan -ne $null); Detail = $AppServicePlanName }

    # API App Service
    $api = az webapp show --name $ApiAppServiceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "API App Service"; Status = ($api -ne $null); Detail = $ApiAppServiceName }

    # Blazor App Service
    $blazor = az webapp show --name $BlazorAppServiceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Blazor App Service"; Status = ($blazor -ne $null); Detail = $BlazorAppServiceName }

    # Storage Account
    $stor = az storage account show --name $StorageAccountName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Storage Account"; Status = ($stor -ne $null); Detail = $StorageAccountName }    

    # Key Vault
    $kv = az keyvault show --name $KeyVaultName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Key Vault"; Status = ($kv -ne $null); Detail = $KeyVaultName }

    # Application Insights (generic ARM query — no extension required)
    $ai = az resource show `
        --resource-group $ResourceGroup `
        --resource-type "Microsoft.Insights/components" `
        --name $ApplicationInsightsName `
        --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Application Insights"; Status = ($ai -ne $null); Detail = $ApplicationInsightsName }

    # Log Analytics
    $la = az monitor log-analytics workspace show --workspace-name $LogAnalyticsWorkspaceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "Log Analytics"; Status = ($la -ne $null); Detail = $LogAnalyticsWorkspaceName }

    # SQL Server (already verified in Phase 6, but check existence here too)
    $sqlSrv = az sql server show --name $SqlServerName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "SQL Server"; Status = ($sqlSrv -ne $null); Detail = $SqlServerName }

    # SQL Database
    $sqlDb = az sql db show --name $SqlDatabaseName --server $SqlServerName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
    $results += [PSCustomObject]@{ Name = "SQL Database"; Status = ($sqlDb -ne $null); Detail = "$SqlServerName/$SqlDatabaseName" }

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
# Phase 8 – Write Summary
# --------------------------------------------------------------------------
function Write-Summary {
    param(
        [PSCustomObject]$DeploymentOutputs,
        [array]$InfrastructureResults
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

# Phase 3
Ensure-AcrPushRoleAssignment `
    -PrincipalId $githubSp.id `
    -Scope $outputs.containerRegistryResourceId `
    -PrincipalName "GitHub Actions"

# Phase 3b
Ensure-DeveloperStorageAccess `
    -StorageAccountResourceId $outputs.storageAccountResourceId    

# Phase 4
Verify-ManagedIdentities `
    -ApiAppName $outputs.apiAppServiceName `
    -BlazorAppName $outputs.blazorAppServiceName

# Phase 5
Verify-AcrPermissions `
    -AcrResourceId $outputs.containerRegistryResourceId `
    -GitHubPrincipalId $githubSp.id `
    -ApiPrincipalId $outputs.apiPrincipalId `
    -BlazorPrincipalId $outputs.blazorPrincipalId

# Phase 6
Verify-SqlInfrastructure `
    -SqlServerName $outputs.sqlServerName `
    -SqlDatabaseName $outputs.sqlDatabaseName

# Phase 7
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
    -SqlDatabaseName $outputs.sqlDatabaseName

# Phase 8
Write-Summary -DeploymentOutputs $outputs -InfrastructureResults $infraResults

exit $EXIT_SUCCESS
