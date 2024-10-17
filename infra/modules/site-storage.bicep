param name string
param location string
param tags object
param containerNames array = [
  'chat-history'
  'pics'
  'thumbnails'
]

resource storageAccounts_resource 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

resource storageAccounts_default_blob 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccounts_resource
  name: 'default'
}

resource storageAccounts_containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for containerName in containerNames: {
    parent: storageAccounts_default_blob
    name: containerName
  }
]
