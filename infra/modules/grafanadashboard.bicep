// --------------------------------------------------------------------------
// Grafana Dashboard (O6 - Azure Workbooks & Grafana Dashboards)
//
// Provisions an ATLAS operational dashboard inside the existing Managed
// Grafana instance via the Microsoft.Dashboard/grafana/dashboards ARM
// sub-resource - fully declarative, no API keys, no post-deployment scripting.
//
// The dashboard definition (Grafana JSON) lives in infra/telemetry/ and is
// loaded at deployment time. Access control follows the Grafana instance's
// Azure AD authorization; the managed identity Monitoring Reader /
// Log Analytics Reader roles (O2) provide data access.
// --------------------------------------------------------------------------

@description('Environment short name: dev, test, prod')
param environment string

@description('Title of the dashboard as shown in Grafana')
param title string

@description('Serialized Grafana dashboard model (JSON). May contain __RESOURCE_GROUP__, __APP_INSIGHTS_NAME__ and __WORKSPACE_ID__ placeholders, which are replaced with the deployed values.')
param serializedData string

@description('Resource group name injected into the dashboard queries')
param resourceGroupName string

@description('Application Insights resource name injected into the dashboard queries')
param applicationInsightsName string

@description('Log Analytics workspace resource ID injected into the dashboard queries')
param logAnalyticsWorkspaceId string

// Reference to the existing Managed Grafana instance (name mirrors names.bicep).
var grafanaNameConst = 'atlas-${environment}-grafana'
resource grafanaInstance 'Microsoft.Dashboard/grafana@2023-09-01' existing = {
  name: grafanaNameConst
}

// Inject deployment-specific values so the deployed dashboard contains no
// unresolved placeholders. Placeholder tokens use double underscores rather
// than {{...}} so they cannot be confused with Grafana runtime variables.
var resolvedData = replace(
  replace(
    replace(serializedData, '__RESOURCE_GROUP__', resourceGroupName),
    '__APP_INSIGHTS_NAME__', applicationInsightsName),
  '__WORKSPACE_ID__', logAnalyticsWorkspaceId)

// Note: a BCP081 warning (no Bicep types for this sub-resource) is expected and
resource grafanaDashboard 'Microsoft.Dashboard/grafana/dashboards@2023-09-01' = {
  parent: grafanaInstance
  name: 'atlas-operations'
  properties: {
    title: title
    serializedData: resolvedData
  }
}

output name string = grafanaDashboard.name
output id   string = grafanaDashboard.id
