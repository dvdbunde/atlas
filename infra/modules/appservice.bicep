// --------------------------------------------------------------------------
// App Service (Web Application)
// --------------------------------------------------------------------------

@description('Name of the App Service')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Resource ID of the App Service Plan')
param planId string

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app'
  properties: {
    serverFarmId: planId
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

resource siteLogs 'Microsoft.Web/sites/config@2023-12-01' = {
  name: 'logs'
  parent: appService
  properties: {
    applicationLogs: {
      fileSystem: {
        level: 'Error'
      }
    }
    httpLogs: {
      fileSystem: {
        enabled: true
        retentionInDays: 7
      }
    }
    failedRequestsTracing: {
      enabled: true
    }
    detailedErrorMessages: {
      enabled: true
    }
  }
}

output id              string = appService.id
output name            string = appService.name
output defaultHostName string = appService.properties.defaultHostName
