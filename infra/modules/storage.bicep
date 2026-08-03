// --------------------------------------------------------------------------
// Storage Account (Azure Blob Storage)
// --------------------------------------------------------------------------

@description('Name of the Storage Account')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Account kind (StorageV2, BlobStorage)')
param kind string = 'StorageV2'

@description('Replication (LRS, GRS, RA-GRS, ZRS)')
param sku string = 'Standard_LRS'

@description('Access tier (Hot, Cool)')
param accessTier string = 'Hot'

@description('Enable blob soft delete retention in days')
param blobSoftDeleteRetentionDays int = 7

@description('Enable container soft delete retention in days')
param containerSoftDeleteRetentionDays int = 7

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  sku: {
    name: sku
  }
  properties: {
    accessTier: accessTier
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: blobSoftDeleteRetentionDays
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: containerSoftDeleteRetentionDays
    }
  }
}

resource permitDocumentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'permit-documents'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

output id                     string = storageAccount.id
output name                   string = storageAccount.name
output primaryBlobEndpoint    string = storageAccount.properties.primaryEndpoints.blob
