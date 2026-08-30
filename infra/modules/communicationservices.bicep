// --------------------------------------------------------------------------
// Azure Communication Services (Email)
// Provides the ACS resource and Azure-managed email domain used by ATLAS
// for email delivery.
//
// The sender address is NOT known during deployment. It will be discovered
// later by the bootstrap script and written into the Blazor App Service
// configuration.
// --------------------------------------------------------------------------

@description('Name of the Azure Communication Services resource')
param name string

@description('Resource tags')
param tags object

@description('Data location for the ACS resource (e.g. Europe)')
param dataLocation string = 'Europe'

// --------------------------------------------------------------------------
// Azure Communication Services
// --------------------------------------------------------------------------

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01-preview' = {
  name: name
  location: 'global'
  tags: tags

  properties: {
    dataLocation: dataLocation
    linkedDomains: [
      emailDomain.id
    ]
  }
}

// --------------------------------------------------------------------------
// Email Service
// --------------------------------------------------------------------------

resource emailService 'Microsoft.Communication/emailServices@2023-04-01-preview' = {
  name: '${name}-email'
  location: 'global'
  tags: tags

  properties: {
    dataLocation: dataLocation
  }
}

// --------------------------------------------------------------------------
// Azure-managed email domain
// --------------------------------------------------------------------------

resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01-preview' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'

  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

// --------------------------------------------------------------------------
// Outputs
// --------------------------------------------------------------------------

output communicationServiceId string = communicationService.id
output communicationServiceName string = communicationService.name
output emailServiceName string = emailService.name
output endpoint string = 'https://${communicationService.name}.communication.azure.com'
