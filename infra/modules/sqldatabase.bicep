// --------------------------------------------------------------------------
// Azure SQL Database
// --------------------------------------------------------------------------

@description('Name of the parent SQL Server')
param serverName string

@description('Name of the SQL Database')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Database SKU / edition (GP_S_Gen5, GP_Gen5, BC_Gen5, S0, S1, etc.)')
param skuName string = 'GP_S_Gen5'

@description('vCores (serverless: 1-4, provisioned: 2-80)')
param capacity int = 1

@description('Serverless auto-pause delay in minutes (-1 = never pause)')
param autoPauseDelay int = 60

@description('Max data size in bytes (default 32GB)')
param maxSizeBytes int = 34359738368

@description('Minimum capacity for serverless (0.5-4)')
param minCapacity int = 1

var databaseName = '${serverName}/${name}'

resource sqlDatabase 'Microsoft.Sql/servers/databases@2025-01-01' = {
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    capacity: capacity
  }
  properties: {
    maxSizeBytes: maxSizeBytes
    autoPauseDelay: autoPauseDelay
    minCapacity: minCapacity
  }
}

output id   string = sqlDatabase.id
output name string = name
