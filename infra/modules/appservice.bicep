// --------------------------------------------------------------------------
// App Service (Linux Web App for Containers)
// Reusable module for container-based web applications.
// Supports Managed Identity, Application Insights, and health checks.
// --------------------------------------------------------------------------

@description('Name of the App Service')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Resource ID of the App Service Plan')
param planId string

@description('Health check path (e.g. /health). Default: /')
param healthCheckPath string = '/'

@description('Container Registry login server (e.g. atlasacr.azurecr.io)')
param acrLoginServer string

@description('Container image repository name (e.g. atlas-api)')
param imageRepository string = ''

@description('Container image tag (e.g. latest, commit SHA)')
param imageTag string = 'latest'

@description('Application settings as key-value pairs')
param appSettings array = []

@description('Enforce HTTPS only')
param httpsOnly bool = true

@description('Enable Always On')
param alwaysOn bool = true

@description('Enable client affinity (ARR cookie)')
param clientAffinityEnabled bool = false

@description('Enable HTTP/2')
param http2Enabled bool = true

// Build the container image string when ACR login server is provided
var linuxFxVersion = !empty(acrLoginServer) ? 'DOCKER|${acrLoginServer}/${imageRepository}:${imageTag}' : ''

// Build the site config
var siteConfig = {
  linuxFxVersion: linuxFxVersion
  alwaysOn: alwaysOn
  healthCheckPath: healthCheckPath
  http20Enabled: http2Enabled
  minTlsVersion: '1.2'
  ftpsState: 'Disabled'
  appSettings: appSettings
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux'
  properties: {
    serverFarmId: planId
    httpsOnly: httpsOnly
    clientAffinityEnabled: clientAffinityEnabled
    siteConfig: siteConfig
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output id            string = appService.id
output name          string = appService.name
output defaultHostName string = appService.properties.defaultHostName
output principalId   string = appService.identity.principalId
output outboundIpAddresses string = appService.properties.outboundIpAddresses
