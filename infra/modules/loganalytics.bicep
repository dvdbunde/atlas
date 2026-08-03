// --------------------------------------------------------------------------
// Log Analytics Workspace
// --------------------------------------------------------------------------

@description('Name of the Log Analytics Workspace')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Pricing tier (PerGB2018, Free, Standard, Premium)')
param sku string = 'PerGB2018'

@description('Data retention in days (30-730)')
param retentionInDays int = 30

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: retentionInDays
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

output id   string = logAnalytics.id
output name string = logAnalytics.name
output workspaceId string = logAnalytics.properties.customerId
