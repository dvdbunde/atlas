// --------------------------------------------------------------------------
// Central tagging module
// Every Azure resource receives these tags. Add project-wide tags here.
// --------------------------------------------------------------------------

@description('Environment short name: dev, test, prod')
param environment string

var resourceTags = {
  Project: 'ATLAS'
  Environment: environment
  ManagedBy: 'Bicep'
  Repository: 'ATLAS'
  Owner: 'Engineering'
}

output tags object = resourceTags
