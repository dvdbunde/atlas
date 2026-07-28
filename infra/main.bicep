// --------------------------------------------------------------------------
// ATLAS Infrastructure — main entry point
// --------------------------------------------------------------------------

targetScope = 'resourceGroup'

// -- Parameters -------------------------------------------------------------
@description('Environment short name: dev, test, prod')
param environment string

@description('Azure region (defaults to resource group location)')
param location string = resourceGroup().location

@description('SQL Server administrator login')
param sqlAdminLogin string

@description('SQL Server administrator login password (secure string)')
@secure()
param sqlAdminPassword string

@description('SKU tier for the App Service Plan')
param appServicePlanSkuTier string = 'Standard'

@description('SKU size for the App Service Plan')
param appServicePlanSkuSize string = 'S1'

@description('App Service Plan instance count')
param appServicePlanCapacity int = 1

@description('SQL Database SKU')
param sqlDatabaseSkuName string = 'GP_S_Gen5'

@description('SQL Database capacity (vCores)')
param sqlDatabaseCapacity int = 1

@description('SQL Database auto-pause delay in minutes')
param sqlDatabaseAutoPauseDelay int = 60

@description('Unique suffix for globally unique names (storage, key vault, SQL server). Leave empty to derive from the subscription ID for deterministic uniqueness.')
param uniqueSuffix string = ''

@description('Storage account replication SKU')
param storageSku string = 'Standard_LRS'

@description('SQL Database max data size in bytes (default 32GB)')
param sqlDatabaseMaxSizeBytes int = 34359738368

@description('Log Analytics retention in days')
param logAnalyticsRetentionInDays int = 30

@description('Enable CanNotDelete resource lock at resource group scope (recommended for production)')
param enableResourceLock bool = false

// -- Central naming & tagging -----------------------------------------------
module names 'modules/names.bicep' = {
  name: '${deployment().name}-names'
  params: {
    environment: environment
    uniqueSuffix: uniqueSuffix
  }
}

module tags 'modules/tags.bicep' = {
  name: '${deployment().name}-tags'
  params: {
    environment: environment
  }
}

// -- Log Analytics Workspace -------------------------------------------------
module logAnalytics 'modules/loganalytics.bicep' = {
  name: '${deployment().name}-loganalytics'
  params: {
    name: names.outputs.logAnalyticsWorkspaceName
    location: location
    tags: tags.outputs.tags
    retentionInDays: logAnalyticsRetentionInDays
  }
}

// -- Application Insights ----------------------------------------------------
module appInsights 'modules/appinsights.bicep' = {
  name: '${deployment().name}-appinsights'
  params: {
    name: names.outputs.applicationInsightsName
    location: location
    tags: tags.outputs.tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// -- User-Assigned Managed Identity ------------------------------------------
module managedIdentity 'modules/managedidentity.bicep' = {
  name: '${deployment().name}-managedidentity'
  params: {
    name: names.outputs.managedIdentityName
    location: location
    tags: tags.outputs.tags
  }
}

// -- App Service Plan --------------------------------------------------------
module appServicePlan 'modules/appserviceplan.bicep' = {
  name: '${deployment().name}-appserviceplan'
  params: {
    name: names.outputs.appServicePlanName
    location: location
    tags: tags.outputs.tags
    skuTier: appServicePlanSkuTier
    skuSize: appServicePlanSkuSize
    capacity: appServicePlanCapacity
  }
}

// -- App Service -------------------------------------------------------------
module appService 'modules/appservice.bicep' = {
  name: '${deployment().name}-appservice'
  params: {
    name: names.outputs.appServiceName
    location: location
    tags: tags.outputs.tags
    planId: appServicePlan.outputs.id
  }
}

// -- SQL Server --------------------------------------------------------------
module sqlServer 'modules/sqlserver.bicep' = {
  name: '${deployment().name}-sqlserver'
  params: {
    name: names.outputs.sqlServerName
    location: location
    tags: tags.outputs.tags
    adminLogin: sqlAdminLogin
    adminLoginPassword: sqlAdminPassword
  }
}

// -- SQL Database ------------------------------------------------------------
module sqlDatabase 'modules/sqldatabase.bicep' = {
  name: '${deployment().name}-sqldatabase'
  params: {
    serverName: names.outputs.sqlServerName
    name: names.outputs.sqlDatabaseName
    location: location
    tags: tags.outputs.tags
    skuName: sqlDatabaseSkuName
    capacity: sqlDatabaseCapacity
    autoPauseDelay: sqlDatabaseAutoPauseDelay
    maxSizeBytes: sqlDatabaseMaxSizeBytes
  }
  dependsOn: [
    sqlServer
  ]
}

// -- Storage Account ---------------------------------------------------------
module storage 'modules/storage.bicep' = {
  name: '${deployment().name}-storage'
  params: {
    name: names.outputs.storageAccountName
    location: location
    tags: tags.outputs.tags
    sku: storageSku
  }
}

// -- Key Vault ---------------------------------------------------------------
module keyVault 'modules/keyvault.bicep' = {
  name: '${deployment().name}-keyvault'
  params: {
    name: names.outputs.keyVaultName
    location: location
    tags: tags.outputs.tags
  }
}

// -- Resource Lock -----------------------------------------------------------
resource resourceLock 'Microsoft.Authorization/locks@2020-05-01' = if (enableResourceLock) {
  name: 'atlas-${environment}-CanNotDelete'
  properties: {
    level: 'CanNotDelete'
    notes: 'Protects ATLAS ${environment} resources from accidental deletion'
  }
}

// -- Outputs -----------------------------------------------------------------
output resourceGroupName            string = names.outputs.resourceGroupName
output appServiceName               string = names.outputs.appServiceName
output appServiceResourceId         string = appService.outputs.id
output appServiceDefaultHostName    string = appService.outputs.defaultHostName
output appServicePlanName           string = names.outputs.appServicePlanName
output sqlServerName                string = names.outputs.sqlServerName
output sqlServerFqdn                string = sqlServer.outputs.fullyQualifiedDomainName
output sqlDatabaseName              string = names.outputs.sqlDatabaseName
output storageAccountName           string = names.outputs.storageAccountName
output storagePrimaryBlobEndpoint   string = storage.outputs.primaryBlobEndpoint
output keyVaultName                 string = names.outputs.keyVaultName
output keyVaultUri                  string = keyVault.outputs.vaultUri
output keyVaultTenantId             string = subscription().tenantId
output applicationInsightsName      string = names.outputs.applicationInsightsName
output applicationInsightsConnectionString string = appInsights.outputs.connectionString
output managedIdentityName          string = names.outputs.managedIdentityName
output managedIdentityPrincipalId   string = managedIdentity.outputs.principalId
output managedIdentityClientId      string = managedIdentity.outputs.clientId
output logAnalyticsWorkspaceName    string = names.outputs.logAnalyticsWorkspaceName
