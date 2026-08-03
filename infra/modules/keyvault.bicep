// --------------------------------------------------------------------------
// Azure Key Vault
// --------------------------------------------------------------------------

@description('Name of the Key Vault')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Soft-delete retention in days (7-90)')
param softDeleteRetentionDays int = 90

@description('SKU name (standard, premium)')
param skuName string = 'standard'

var vaultSuffix = environment().suffixes.keyvaultDns
var vaultUri = 'https://${name}${vaultSuffix}'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    tenantId: subscription().tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionDays
    enableRbacAuthorization: true
    enablePurgeProtection: true
  }
}

output id        string = keyVault.id
output name      string = keyVault.name
output vaultUri  string = vaultUri
output tenantId  string = subscription().tenantId
