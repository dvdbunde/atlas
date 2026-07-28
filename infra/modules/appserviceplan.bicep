// --------------------------------------------------------------------------
// Application Service Plan
// --------------------------------------------------------------------------

@description('Name of the App Service Plan')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('SKU tier (Free, Basic, Standard, Premium)')
param skuTier string = 'Standard'

@description('SKU size (B1, S1, S2, P1v2, P1v3, etc.)')
param skuSize string = 'S1'

@description('Number of worker instances')
param capacity int = 1

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    tier: skuTier
    name: skuSize
    capacity: capacity
  }
  kind: 'app'
  properties: {
    reserved: false // Windows
  }
}

output id   string = plan.id
output name string = plan.name
