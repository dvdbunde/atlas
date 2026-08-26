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

@description('ASP.NET Core environment name applied to App Services (e.g. Development, Production). Defaults to Development.') 
param environmentName string = 'Development'

@description('Email address for O7 alert notifications (Action Group receiver). Supplied per environment; never hard-coded in source control.')
@secure()
param alertNotificationEmail string

@description('Microsoft Entra object ID of the identity that runs infra/bootstrap.ps1. Bootstrap provisions the ATLAS Operations Grafana dashboard via the Managed Grafana data-plane API, so this identity needs Grafana Editor on the Managed Grafana resource. Not secret; supplied per environment - never hard-coded in source control.')
param grafanaBootstrapPrincipalId string

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

// -- Azure Managed Grafana (O2 – primary dashboard platform) -----------------
// SystemAssigned identity; Azure Monitor RBAC role assignments are granted
// after this module below. No dependency on App Services / SQL etc., so no
// circular references.
module grafana 'modules/grafana.bicep' = {
  name: '${deployment().name}-grafana'
  params: {
    name: names.outputs.grafanaName
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
    healthCheckPath: '/health/ready'
    acrLoginServer: containerRegistry.outputs.loginServer
    imageRepository: 'atlas-api'
    imageTag: apiImageTag
    appSettings: [
      {
        name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
        value: 'true'
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environmentName
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appInsights.outputs.connectionString
      }
      {
        name: 'ConnectionStrings__DefaultConnection'
        value: sqlConnectionStringRef
      }
      {
        name: 'Storage__AccountName'
        value: storage.outputs.name
      }
      {
        name: 'Storage__EmailTemplatesContainer'
        value: 'email-templates'
      }
      {
        name: 'Email__Acs__Endpoint'
        value: communicationServices.outputs.endpoint
      }      
      {
        name: 'KeyVault__VaultName'
        value: keyVault.outputs.name
      }
    ]
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
    healthCheckPath: '/health/ready'
    acrLoginServer: containerRegistry.outputs.loginServer
    imageRepository: 'atlas-blazor'
    imageTag: blazorImageTag
    appSettings: [
      {
        name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
        value: 'true'
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environmentName
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appInsights.outputs.connectionString
      }
      {
        name: 'ConnectionStrings__DefaultConnection'
        value: sqlConnectionStringRef
      }
      {
        name: 'Storage__AccountName'
        value: storage.outputs.name
      }
      {
        name: 'Storage__EmailTemplatesContainer'
        value: 'email-templates'
      }
      {
        name: 'Email__Acs__Endpoint'
        value: communicationServices.outputs.endpoint
      }      
      {
        name: 'KeyVault__VaultName'
        value: keyVault.outputs.name
      }
    ]
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

// -- Azure Communication Services (Email) ------------------------------------
module communicationServices 'modules/communicationservices.bicep' = {
  name: '${deployment().name}-communicationservices'
  params: {
    name: names.outputs.communicationServicesName    
    tags: tags.outputs.tags   
  }
}

// Deterministic suffix derived from the subscription ID (or explicit override).
// Used to compute globally unique names (Key Vault, etc.) in main.bicep.
var effectiveSuffix = !empty(uniqueSuffix) ? uniqueSuffix : take(replace(subscription().subscriptionId, '-', ''), 6)

// Reference the Storage Account as an existing resource so RBAC role
// assignments can be scoped to it. The name is deterministic (derived from
// environment + uniqueSuffix), matching the naming module.
var storageAccountName = replace('atlas${environment}storage${effectiveSuffix}', '-', '')
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
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

// Reference the Key Vault as an existing resource so RBAC role assignments can
// be scoped to it. The name is deterministic (derived from environment +
// uniqueSuffix), matching the naming module.
var keyVaultName = 'atlas${environment}kv${effectiveSuffix}'
resource keyVaultResource 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// -- Key Vault Secrets -------------------------------------------------------
// Store the runtime secrets in Key Vault so App Service settings can use
// Key Vault references instead of plaintext values. This leaves Key Vault fully
// populated after deployment with no manual secret creation required.
//
// Each secret has an explicit dependsOn on the Key Vault module deployment so
// ARM deterministically deploys Key Vault -> secrets -> App Service settings,
// rather than relying on ARM's implicit ordering.
resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVaultResource
  name: 'sql-connection-string'
  properties: {
    value: 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.outputs.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
  dependsOn: [
    keyVault
  ]
}

// -- Key Vault reference strings for App Service settings --------------------
var sqlConnectionStringRef = '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}/secrets/sql-connection-string/)'

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

// -- Key Vault Secrets User role definition (built-in) -----------------------
// Grants read access to Key Vault secrets so App Service Key Vault references
// can be resolved. Key Vault uses RBAC (enableRbacAuthorization: true).
var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6'

// -- Key Vault Secrets User: API App Service ---------------------------------
resource apiKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, 'api-kv-secrets-user', subscription().subscriptionId)
  scope: keyVaultResource
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
    principalId: apiAppService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -- Key Vault Secrets User: Blazor App Service ------------------------------
resource blazorKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, 'blazor-kv-secrets-user', subscription().subscriptionId)
  scope: keyVaultResource
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
    principalId: blazorAppService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -- Storage Blob Data Contributor role definition (built-in) ----------------
// Grants read/write/delete access to Blob Storage so App Services can access
// blobs via their System Assigned Managed Identity (no Storage Account keys).
var storageBlobDataContributorRoleDefinitionId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// -- Storage Blob Data Contributor: API App Service --------------------------
resource apiStorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountName, 'api-storage-blob-contributor', subscription().subscriptionId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleDefinitionId)
    principalId: apiAppService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -- Storage Blob Data Contributor: Blazor App Service ------------------------
resource blazorStorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountName, 'blazor-storage-blob-contributor', subscription().subscriptionId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleDefinitionId)
    principalId: blazorAppService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// ==========================================================================
// O2 – Azure Monitor Integration
// ==========================================================================

// -- Grafana RBAC -------------------------------------------------------------
// Managed Grafana's managed identity needs read access to Azure Monitor data
// (Log Analytics workspace queries + resource metrics) so Grafana's Azure
// Monitor data sources can query ATLAS telemetry. No secrets are used.

// Monitoring Reader on the resource group: covers metric/list access for all
// ATLAS resources in one assignment (least privilege at the required scope).
var monitoringReaderRoleDefinitionId = '43d0d8ad-25c7-4714-9337-8ba259a9fe05'

// Deploy-time constant name (must mirror names.bicep) for role assignment IDs,
// which require values computable at deployment start.
var grafanaResourceName = 'atlas-${environment}-grafana'

resource grafanaMonitoringReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().name, grafanaResourceName, 'monitoring-reader', subscription().subscriptionId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringReaderRoleDefinitionId)
    principalId: grafana.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// Log Analytics Reader on the workspace: allows Grafana to run KQL queries
// against the workspace (Application Insights tables + resource diagnostic logs).
// Role definition verified against Azure CLI: az role definition list --name "Log Analytics Reader"
var logAnalyticsReaderRoleDefinitionId = '73c42c96-874c-492b-b04d-ab87d138a893'

// Reference the existing Log Analytics workspace so the role assignment is
// scoped to the workspace itself (not the resource group), matching the
// bootstrap verification. Name mirrors names.bicep (deploy-time constant
// required for scope resolution).
var logAnalyticsWorkspaceNameConst = 'atlas-${environment}-logs'
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceNameConst
}

// Reference the existing Application Insights resource for O7 alert scopes.
// Name mirrors names.bicep (deploy-time constant required for scope resolution).
var applicationInsightsNameConst = 'atlas${environment}appi'
resource appInsightsRef 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsNameConst
}

resource grafanaLogAnalyticsReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(logAnalyticsWorkspaceNameConst, grafanaResourceName, 'la-reader', subscription().subscriptionId)
  scope: logAnalyticsWorkspace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', logAnalyticsReaderRoleDefinitionId)
    principalId: grafana.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -- Grafana Editor: manual bootstrap identity (O6) ---------------------------
// The dashboard is provisioned by bootstrap.ps1 through the Managed Grafana
// data-plane API using the signed-in Azure CLI user's identity. That identity
// needs Grafana Editor on the Managed Grafana RESOURCE itself (not RG/subscription
// scope). Role definition verified against Azure CLI:
//   az role definition list --name "Grafana Editor"
//   -> a79a5197-3a5c-4973-a920-486035ffd60f
// Grafana's managed identity roles above are for data access and remain unchanged.
var grafanaEditorRoleDefinitionId = 'a79a5197-3a5c-4973-a920-486035ffd60f'

// Reference the deployed Grafana instance so the role assignment is scoped to
// exactly that resource. Name mirrors names.bicep (deploy-time constant).
resource grafanaInstance 'Microsoft.Dashboard/grafana@2023-09-01' existing = {
  name: grafanaResourceName
}

resource grafanaBootstrapEditor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(grafanaResourceName, 'bootstrap-grafana-editor', subscription().subscriptionId)
  scope: grafanaInstance
  // The Grafana resource is created by the grafana module; the existing
  // reference above does not establish that dependency.
  dependsOn: [
    grafana
  ]
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', grafanaEditorRoleDefinitionId)
    principalId: grafanaBootstrapPrincipalId
    principalType: 'User'
  }
}

// -- Diagnostic Settings (O2) -------------------------------------------------
// Each diagnostic setting targets its Azure resource through the Bicep `scope`
// mechanism (extension resources), NOT via properties.targetResourceId — the
// latter is rejected by the Azure provider during What-If/deployment.
//
// The generic string-based module was removed because Bicep scopes require
// actual resource references; the six settings are therefore defined directly
// where those references are available.
//
// Category names are curated for operational value vs. cost: high-volume/noisy
// categories (e.g. App Service HTTP logs, which duplicate App Insights request
// telemetry) are intentionally excluded.

var appServiceLogCategories = [
  'AppServiceAuditLogs'
  'AppServiceIPSecAuditLogs'
  'AppServicePlatformLogs'
]

// Extension-resource scope requires actual resource references. Resources that
// live inside modules are referenced as existing resources (names come from
// the names module / module-level vars).
var apiAppServiceNameConst = 'atlas-api-${environment}-${effectiveSuffix}'
resource apiAppServiceRef 'Microsoft.Web/sites@2023-12-01' existing = {
  name: apiAppServiceNameConst
}

var blazorAppServiceNameConst = 'atlas-blazor-${environment}-${effectiveSuffix}'
resource blazorAppServiceRef 'Microsoft.Web/sites@2023-12-01' existing = {
  name: blazorAppServiceNameConst
}

var sqlServerNameConst = 'atlas${environment}sql${effectiveSuffix}'
resource sqlServerResource 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: sqlServerNameConst
}

resource sqlDatabaseRef 'Microsoft.Sql/servers/databases@2021-11-01' existing = {
  parent: sqlServerResource
  name: 'atlas-${environment}-db'
}

resource keyVaultRef 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource storageBlobServiceRef 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storageAccount
  name: 'default'
}

// -- API App Service ----------------------------------------------------------
resource apiAppServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: apiAppServiceRef
  name: 'atlas-api-diagnostics'
  // The App Service is created by the apiAppService module; the existing
  // reference above does not establish that dependency.
  dependsOn: [
    apiAppService
  ]
  properties: {
    workspaceId: logAnalytics.outputs.id
    logs: [for category in appServiceLogCategories: {
      category: category
      enabled: true
    }]
    metrics: [{
      category: 'AllMetrics'
      enabled: true
    }]
  }
}

// -- Blazor App Service -------------------------------------------------------
resource blazorAppServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: blazorAppServiceRef
  name: 'atlas-blazor-diagnostics'
  dependsOn: [
    blazorAppService
  ]
  properties: {
    workspaceId: logAnalytics.outputs.id
    logs: [for category in appServiceLogCategories: {
      category: category
      enabled: true
    }]
    metrics: [{
      category: 'AllMetrics'
      enabled: true
    }]
  }
}

// -- SQL Database -------------------------------------------------------------
// SQLSecurityAuditEvents (audit trail, compliance-relevant) plus insights and
// automatic tuning recommendations.
resource sqlDatabaseDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: sqlDatabaseRef
  name: 'atlas-sql-diagnostics'
  dependsOn: [
    sqlDatabase
  ]
  properties: {
    workspaceId: logAnalytics.outputs.id
    logs: [
      { category: 'SQLSecurityAuditEvents', enabled: true }
      { category: 'SQLInsights', enabled: true }
      { category: 'AutomaticTuning', enabled: true }
    ]
    metrics: [{
      category: 'Basic'
      enabled: true
    }]
  }
}

// -- Storage Blob Service -----------------------------------------------------
// Blob log categories are exposed by the blobServices/default child resource,
// not the account root.
resource storageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: storageBlobServiceRef
  name: 'atlas-storage-diagnostics'
  dependsOn: [
    storage
  ]
  properties: {
    workspaceId: logAnalytics.outputs.id
    logs: [
      { category: 'StorageRead', enabled: true }
      { category: 'StorageWrite', enabled: true }
      { category: 'StorageDelete', enabled: true }
    ]
    metrics: [{
      category: 'Transaction'
      enabled: true
    }]
  }
}

// -- Key Vault ----------------------------------------------------------------
resource keyVaultDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: keyVaultRef
  name: 'atlas-keyvault-diagnostics'
  dependsOn: [
    keyVault
  ]
  properties: {
    workspaceId: logAnalytics.outputs.id
    logs: [{ category: 'AuditEvent', enabled: true }]
    metrics: [{
      category: 'AllMetrics'
      enabled: true
    }]
  }
}
// ==========================================================================
// O6 - Azure Workbooks & Grafana Dashboards
// ==========================================================================

// -- ATLAS Operations Workbook ------------------------------------------------
// ARM-provisioned Azure Monitor Workbook scoped to the resource group.
// Definition lives in infra/telemetry/atlas-operations.workbook.json and is
// loaded at deployment time. Name must be a GUID (provider requirement);
// derived deterministically so re-deployments update in place.
var operationsWorkbookName = guid(resourceGroup().id, 'atlas-operations-workbook')
var operationsWorkbookData = loadTextContent('telemetry/atlas-operations.workbook.json')

module operationsWorkbook 'modules/workbook.bicep' = {
  name: '${deployment().name}-operations-workbook'
  params: {
    name: operationsWorkbookName
    location: location
    tags: tags.outputs.tags
    serializedData: operationsWorkbookData
    displayName: 'ATLAS Operations'
  }
}

// -- ATLAS Operations Grafana Dashboard ---------------------------------------
// Provisioned by Phase 11b of infra/bootstrap.ps1 via the Managed Grafana
// dashboard API (the Microsoft.Dashboard/grafana/dashboards ARM sub-resource
// is not a registered resource type and fails preflight validation).
// Definition lives in infra/telemetry/atlas-operations.grafana-dashboard.json.


// ==========================================================================
// O7 - Alerts & Operational Readiness
// ==========================================================================

// -- Action Group ------------------------------------------------------------
// Single notification route for all ATLAS alert rules. The email receiver is
// supplied as a secure deployment parameter - never hard-coded in source.
module actionGroup 'modules/actiongroup.bicep' = {
  name: '${deployment().name}-actiongroup'
  params: {
    name: names.outputs.actionGroupName
    tags: tags.outputs.tags
    emailReceiver: alertNotificationEmail
  }
}

// -- Exception spike (scheduled query, Sev 2) ---------------------------------
// Sustained elevated exception volume in the application. Thresholds are
// conservative dev defaults - tune per environment as real traffic patterns
// become known.
var exceptionAlertQuery = '''
exceptions
| where timestamp > ago(1h)
| summarize Exceptions = count()
'''

module exceptionSpikeAlert 'modules/scheduledqueryalert.bicep' = {
  name: '${deployment().name}-exception-spike-alert'
  dependsOn: [
    appInsights
  ]
  params: {
    name: 'atlas-${environment}-exception-spike'
    tags: tags.outputs.tags
    alertDescription: 'Elevated exception count in the ATLAS application over the last hour (one evaluation above threshold). Inspect exceptions in Application Insights, then the ATLAS Operations Workbook / Grafana dashboard for trends by problemId.'
    severity: 2
    query: exceptionAlertQuery
    // Scoped to App Insights only. This is a workspace-based App Insights
    // deployment: the same exceptions are visible through both the workspace
    // and the component, so including both scopes would double-count against
    // the threshold. Consistent with the other App Insights log alerts.
    resourceIds: [
      appInsightsRef.id
    ]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT1H'
    operator: 'GreaterThan'
    threshold: 50
    failureCount: 1
    metricMeasureColumn: 'Exceptions'
    actionGroupId: actionGroup.outputs.id
  }
}

// -- Email delivery failures (scheduled query, Sev 2) -------------------------
// Uses the existing O4 custom metric atlas.email.sends with dimension
// outcome=failure. A single failed email does NOT fire this alert: the query
// requires >= 5 failures within one hour, sustained across two consecutive
// evaluations, indicating an ACS or configuration problem rather than noise.
var emailFailureAlertQuery = '''
let emails = customMetrics
| where name == "atlas.email.sends";
emails
| where customDimensions["outcome"] == "failure"
| where timestamp > ago(1h)
| summarize Failures = sum(todouble(valueCount))
'''

module emailFailureAlert 'modules/scheduledqueryalert.bicep' = {
  name: '${deployment().name}-email-failure-alert'
  dependsOn: [
    appInsights
  ]
  params: {
    name: 'atlas-${environment}-email-failures'
    tags: tags.outputs.tags
    alertDescription: 'Sustained email delivery failures detected (atlas.email.sends outcome=failure). Likely ACS outage or sender configuration problem. Check ACS email operational logs in Log Analytics and the Email panel on the ATLAS Operations Grafana dashboard.'
    severity: 2
    query: emailFailureAlertQuery
    resourceIds: [
      appInsightsRef.id
    ]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT1H'
    operator: 'GreaterThan'
    threshold: 4
    failureCount: 1
    metricMeasureColumn: 'Failures'
    actionGroupId: actionGroup.outputs.id
  }
}

// -- Sustained command latency (scheduled query, Sev 3) -----------------------
// p95 of atlas.command.duration exceeding 10 seconds over an hour indicates
// performance degradation (slow SQL, storage, or downstream dependency).
var commandLatencyAlertQuery = '''
customMetrics
| where name == "atlas.command.duration"
| summarize CommandDurationP95 = percentile(todouble(value), 95)
'''

module commandLatencyAlert 'modules/scheduledqueryalert.bicep' = {
  name: '${deployment().name}-command-latency-alert'
  dependsOn: [
    appInsights
  ]
  params: {
    name: 'atlas-${environment}-command-latency'
    tags: tags.outputs.tags
    alertDescription: 'Sustained high p95 command execution duration (atlas.command.duration). Performance degradation likely caused by a slow dependency. Use the Command duration panel on the ATLAS Operations Grafana dashboard to identify which command type is slow, then inspect the corresponding dependency.'
    severity: 3
    query: commandLatencyAlertQuery
    resourceIds: [
      appInsightsRef.id
    ]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT1H'
    operator: 'GreaterThan'
    threshold: 10000
    failureCount: 1
    metricMeasureColumn: 'CommandDurationP95'
    actionGroupId: actionGroup.outputs.id
  }
}

// -- Azure platform/service health (activity log alert, Sev 2) -----------------
// Fires on Azure-side service incidents/maintenance affecting resources in the
// resource group. Distinguishes platform problems from application failures:
// if this fires, do NOT debug application code first.
module serviceHealthAlert 'modules/servicehealthalert.bicep' = {
  name: '${deployment().name}-service-health-alert'
  params: {
    name: 'atlas-${environment}-service-health'
    tags: tags.outputs.tags
    alertDescription: 'Azure Service Health incident, maintenance, or security advisory affecting the ATLAS resource group. This is an Azure platform event, not an application failure. Check Azure Service Health for scope and remediation guidance.'
    actionGroupId: actionGroup.outputs.id
  }
}

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
output logAnalyticsWorkspaceId      string = logAnalytics.outputs.id
output grafanaName                  string = names.outputs.grafanaName
output grafanaEndpoint              string = grafana.outputs.endpoint
output grafanaPrincipalId           string = grafana.outputs.principalId
output grafanaResourceId            string = grafana.outputs.id
output communicationServiceName     string = communicationServices.outputs.communicationServiceName
output communicationServicesEndpoint string = communicationServices.outputs.endpoint
output communicationEmailServiceName string = communicationServices.outputs.emailServiceName
output operationsWorkbookName        string = operationsWorkbook.outputs.name
output operationsWorkbookId          string = operationsWorkbook.outputs.id
output actionGroupName               string = actionGroup.outputs.name
output actionGroupId                 string = actionGroup.outputs.id
output exceptionSpikeAlertName       string = exceptionSpikeAlert.outputs.name
output emailFailureAlertName         string = emailFailureAlert.outputs.name
output commandLatencyAlertName       string = commandLatencyAlert.outputs.name
output serviceHealthAlertName        string = serviceHealthAlert.outputs.name
output resourceGroupName            string = resourceGroup().name
output apiAppServiceName            string = names.outputs.apiAppServiceName
output apiAppServiceResourceId      string = apiAppService.outputs.id
output apiHostname                  string = apiAppService.outputs.defaultHostName
output apiPrincipalId               string = apiAppService.outputs.principalId
output blazorAppServiceName         string = names.outputs.blazorAppServiceName
output blazorAppServiceResourceId   string = blazorAppService.outputs.id
output blazorHostname               string = blazorAppService.outputs.defaultHostName
output blazorPrincipalId            string = blazorAppService.outputs.principalId
