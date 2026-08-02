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

@description('API container image tag. Defaults to commit SHA in CI/CD.')
param apiImageTag string = 'latest'

@description('Blazor container image tag. Defaults to commit SHA in CI/CD.')
param blazorImageTag string = 'latest'

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

// -- Azure Container Registry ------------------------------------------------
module containerRegistry 'modules/containerregistry.bicep' = {
  name: '${deployment().name}-acr'
  params: {
    name: names.outputs.containerRegistryName
    location: location
    tags: tags.outputs.tags
  }
}

// Reference the existing ACR for resource-scoped role assignments.
// Bicep requires the `name` to be a compile-time constant for resource-scoped
// role assignments. The ACR name is deterministic from the naming module.
var acrName = 'atlasacrde96db'
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// -- API App Service (Linux Container) ---------------------------------------
module apiAppService 'modules/appservice.bicep' = {
  name: '${deployment().name}-api-appservice'
  params: {
    name: names.outputs.apiAppServiceName
    location: location
    tags: tags.outputs.tags
    planId: appServicePlan.outputs.id
    healthCheckPath: '/health'
    acrLoginServer: containerRegistry.outputs.loginServer
    imageRepository: 'atlas-api'
    imageTag: apiImageTag
  }
}

// -- Blazor App Service (Linux Container) ------------------------------------
module blazorAppService 'modules/appservice.bicep' = {
  name: '${deployment().name}-blazor-appservice'
  params: {
    name: names.outputs.blazorAppServiceName
    location: location
    tags: tags.outputs.tags
    planId: appServicePlan.outputs.id
    healthCheckPath: '/'
    acrLoginServer: containerRegistry.outputs.loginServer
    imageRepository: 'atlas-blazor'
    imageTag: blazorImageTag
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

// -- AcrPull role definition (built-in) --------------------------------------
var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

// -- AcrPull: API App Service ------------------------------------------------
resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {  
  name: guid(acrName, 'api-acrpull', subscription().subscriptionId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleDefinitionId)
    principalId: apiAppService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -- AcrPull: Blazor App Service ---------------------------------------------
resource blazorAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrName, 'blazor-acrpull', subscription().subscriptionId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleDefinitionId)
    principalId: blazorAppService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -- Outputs -----------------------------------------------------------------
output resourceGroupName            string = names.outputs.resourceGroupName
output apiAppServiceName            string = names.outputs.apiAppServiceName
output apiAppServiceResourceId      string = apiAppService.outputs.id
output apiHostname                  string = apiAppService.outputs.defaultHostName
output apiPrincipalId               string = apiAppService.outputs.principalId
output blazorAppServiceName         string = names.outputs.blazorAppServiceName
output blazorAppServiceResourceId   string = blazorAppService.outputs.id
output blazorHostname               string = blazorAppService.outputs.defaultHostName
output blazorPrincipalId            string = blazorAppService.outputs.principalId
output containerRegistryName        string = names.outputs.containerRegistryName
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output containerRegistryResourceId  string = containerRegistry.outputs.id
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
output logAnalyticsWorkspaceName    string = names.outputs.logAnalyticsWorkspaceName
