targetScope = 'resourceGroup'

param name string = 'atlas-comm-dev-test'
param dataLocation string = 'Europe'

module communicationServices 'modules/communicationservices.bicep' = {
  name: 'communicationservices'
  params: {
    name: name
    dataLocation: dataLocation
    tags: {}
  }
}

output endpoint string = communicationServices.outputs.endpoint
