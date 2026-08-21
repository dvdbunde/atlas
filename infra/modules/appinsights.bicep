// --------------------------------------------------------------------------
// Application Insights
// --------------------------------------------------------------------------

@description('Name of the Application Insights resource')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Resource ID of the Log Analytics Workspace')
param logAnalyticsWorkspaceId string

@description('Application type (web, other)')
param applicationType string = 'web'

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
    DisableIpMasking: false
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output id                 string = appInsights.id
output name               string = appInsights.name
output connectionString   string = appInsights.properties.ConnectionString
