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
var appServicePlanName = 'atlas-${environment}-plan'
var appServiceName     = 'atlas-${environment}-app${effectiveSuffix}'

// -- Database ---------------------------------------------------------------
var sqlServerName   = 'atlas${environment}sql${effectiveSuffix}'
var sqlDatabaseName = 'atlas-${environment}-db'

// -- Storage ----------------------------------------------------------------
var storageAccountName = replace('atlas${environment}storage${effectiveSuffix}', '-', '')

// -- Security ---------------------------------------------------------------
var keyVaultName = 'atlas${environment}kv${effectiveSuffix}'

// -- Identity ---------------------------------------------------------------
var managedIdentityName = 'atlas-${environment}-mi'

// -- Observability ----------------------------------------------------------
var logAnalyticsWorkspaceName = 'atlas-${environment}-logs'
var applicationInsightsName   = 'atlas${environment}appi'

// ---------------------------------------------------------------------------
// Exports
// ---------------------------------------------------------------------------
output resourceGroupName          string = resourceGroupName
output appServicePlanName         string = appServicePlanName
output appServiceName             string = appServiceName
output sqlServerName              string = sqlServerName
output sqlDatabaseName            string = sqlDatabaseName
output storageAccountName         string = storageAccountName
output keyVaultName               string = keyVaultName
output managedIdentityName        string = managedIdentityName
output logAnalyticsWorkspaceName  string = logAnalyticsWorkspaceName
output applicationInsightsName    string = applicationInsightsName
