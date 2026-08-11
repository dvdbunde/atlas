// --------------------------------------------------------------------------
// Azure Communication Services (Email)
// Provides the ACS resource and email domain used by ATLAS for email delivery.
// No connection strings or access keys are exposed; the application authenticates
// via Managed Identity (DefaultAzureCredential).
// --------------------------------------------------------------------------

@description('Name of the Azure Communication Services resource')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Email domain name (e.g. atlas.com). The sender address is DoNotReply@<domain>.')
param emailDomainName string

@description('Data location for the ACS resource (e.g. United States, Europe)')
param dataLocation string = 'United States'

// -- Azure Communication Services resource ----------------------------------
resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// -- Email Service (domain) -------------------------------------------------
resource emailService 'Microsoft.Communication/emailServices@2023-04-01-preview' = {
  name: '${name}-email'
  location: location
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// -- Email Domain (verified sender domain) ----------------------------------
resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01-preview' = {
  parent: emailService
  name: emailDomainName
  location: location
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

output id                 string = communicationService.id
output name               string = communicationService.name
output endpoint           string = 'https://${communicationService.name}.communication.azure.com'
output emailDomainName    string = emailDomainName
output senderAddress      string = 'DoNotReply@${emailDomainName}'
