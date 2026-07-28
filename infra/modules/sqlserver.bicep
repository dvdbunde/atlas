// --------------------------------------------------------------------------
// Azure SQL Server
// --------------------------------------------------------------------------

@description('Name of the SQL Server')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('SQL Administrator login username')
param adminLogin string

@description('SQL Administrator login password. Use a strong password; rotate via Key Vault in later phases.')
@secure()
param adminLoginPassword string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

output id                       string = sqlServer.id
output name                     string = sqlServer.name
output fullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
