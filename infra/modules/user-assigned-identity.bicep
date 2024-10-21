@description('Location for the creation of the identity, takes the location of the resource group by default')
param location string = resourceGroup().location
param tags object

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'uai-deployment-static-${substring(uniqueString(resourceGroup().id),0, 4)}'
  location: location
  tags: tags
}

// Assign the identity the "Reader" role on the resource group
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('acdd72a7-3385-48ef-bd42-f606fba81ae7', identity.name, subscription().subscriptionId, resourceGroup().name)
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', 'acdd72a7-3385-48ef-bd42-f606fba81ae7')
    principalType: 'ServicePrincipal'
  }
}

@description('Generated name for the identity')
output identity_name string = identity.name
@description('Generated id for the identity')
output identity_id string = identity.id
