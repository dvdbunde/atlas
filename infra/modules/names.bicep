// --------------------------------------------------------------------------
// Central naming convention module
// All resource names are derived from environment + project prefix.
// --------------------------------------------------------------------------

@description('The environment short name: dev, test, prod')
param environment string

@description('Unique suffix for globally unique names (storage, key vault, sql server). When empty, derived from the subscription ID for deterministic uniqueness.')
param uniqueSuffix string = ''

// Derive a deterministic suffix from the subscription ID when not provided.
// This avoids deployment failures due to globally unique name collisions
// while remaining deterministic across redeployments.
var effectiveSuffix = !empty(uniqueSuffix) ? uniqueSuffix : take(replace(subscription().subscriptionId, '-', ''), 6)

// -- Resource Group ---------------------------------------------------------
var resourceGroupName = 'atlas-${environment}-rg'

// -- Compute ----------------------------------------------------------------
var appServicePlanName    = 'atlas-${environment}-plan'
var apiAppServiceName     = 'atlas-api-${environment}-${effectiveSuffix}'
var blazorAppServiceName  = 'atlas-blazor-${environment}-${effectiveSuffix}'

// -- Container Registry -----------------------------------------------------
var containerRegistryName = 'atlasacr${effectiveSuffix}'

// -- Database ---------------------------------------------------------------
var sqlServerName   = 'atlas${environment}sql${effectiveSuffix}'
var sqlDatabaseName = 'atlas-${environment}-db'

// -- Storage ----------------------------------------------------------------
var storageAccountName = replace('atlas${environment}storage${effectiveSuffix}', '-', '')

// -- Communication Services -------------------------------------------------
var communicationServicesName = 'atlas-comm-${environment}-${effectiveSuffix}'

// -- Security ---------------------------------------------------------------
var keyVaultName = 'atlas${environment}kv${effectiveSuffix}'

// -- Observability ----------------------------------------------------------
var logAnalyticsWorkspaceName = 'atlas-${environment}-logs'
var applicationInsightsName   = 'atlas${environment}appi'
var grafanaName               = 'atlas-${environment}-grafana'

// -- Alerting (O7 – Alerts & Operational Readiness) ---------------------------
var actionGroupName           = 'atlas-${environment}-ops-ag'

// ---------------------------------------------------------------------------
// Exports
// ---------------------------------------------------------------------------
output resourceGroupName          string = resourceGroupName
output appServicePlanName         string = appServicePlanName
output apiAppServiceName          string = apiAppServiceName
output blazorAppServiceName       string = blazorAppServiceName
output containerRegistryName      string = containerRegistryName
output sqlServerName              string = sqlServerName
output sqlDatabaseName            string = sqlDatabaseName
output storageAccountName         string = storageAccountName
output communicationServicesName  string = communicationServicesName
output keyVaultName               string = keyVaultName
output logAnalyticsWorkspaceName  string = logAnalyticsWorkspaceName
output applicationInsightsName    string = applicationInsightsName
output grafanaName                string = grafanaName
output actionGroupName            string = actionGroupName
