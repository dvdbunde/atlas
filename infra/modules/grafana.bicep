// --------------------------------------------------------------------------
// Azure Managed Grafana (O2 – Azure Monitor Integration)
//
// Primary operational dashboard platform for ATLAS (per ADR-023 and the
// Milestone 11 architecture). Uses SystemAssigned Managed Identity — no
// passwords or stored credentials. RBAC role assignments granting access to
// Azure Monitor data are deployed separately in main.bicep (after this
// module) to avoid circular dependencies.
// --------------------------------------------------------------------------

@description('Name of the Azure Managed Grafana instance')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Grafana SKU tier (Standard is the only generally-available tier)')
param sku string = 'Standard'

@description('Enable zone redundancy (production recommendation)')
param zoneRedundancy string = 'Disabled'

@description('Grafana major version')
param grafanaMajorVersion int = 10

resource grafana 'Microsoft.Dashboard/grafana@2023-09-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    grafanaMajorVersion: string(grafanaMajorVersion)
    zoneRedundancy: zoneRedundancy
    apiKey: 'Disabled'
    deterministicOutboundIP: 'Disabled'
  }
}

output id          string = grafana.id
output name        string = grafana.name
output principalId string = grafana.identity.principalId
output endpoint    string = grafana.properties.endpoint
