// --------------------------------------------------------------------------
// Azure Container Registry
// Provisions a single Azure Container Registry for Docker image storage.
// Admin user disabled — Azure AD authentication only.
// --------------------------------------------------------------------------

@description('Name of the Container Registry')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('SKU tier for the Container Registry')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Basic'
param retentionDays int = 7
param enableRetention bool = true

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Disabled'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: retentionDays
        status: enableRetention ? 'enabled' : 'disabled'
      }
    }
  }
}

output name          string = containerRegistry.name
output loginServer   string = containerRegistry.properties.loginServer
output id            string = containerRegistry.id
