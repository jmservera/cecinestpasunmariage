/// https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deployment-script-bicep?tabs=CLI

@description('Location for the creation of the identity, takes the location of the resource group by default')
param location string = resourceGroup().location

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'uai-deployment-static-${substring(uniqueString(resourceGroup().id),0, 4)}'
  location: location
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

// Creates the custom role to access the actions needed for the deployment script
// Using least privilege principle
resource customRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' = {
  name: guid(
    'deployment-script-minimum-privilege-for-deployment-principal',
    subscription().subscriptionId,
    resourceGroup().name
  )
  scope: resourceGroup()
  properties: {
    roleName: '${resourceGroup().name}-deployment-script-minimum-privilege-for-deployment-principal'
    description: 'Configure least privilege for the deployment principal in deployment script'
    type: 'customRole'
    permissions: [
      {
        actions: [
          'Microsoft.Web/staticSites/customDomains/validate/action'
        ]
      }
    ]
    assignableScopes: [
      resourceGroup().id
    ]
  }
}

// assign the custom role to the identity
resource roleAssignmentCustomRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    'deployment-script-minimum-privilege-for-deployment-principal',
    identity.name,
    subscription().subscriptionId,
    resourceGroup().name
  )
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: customRole.id
    principalType: 'ServicePrincipal'
  }
}

@description('Generated name for the identity')
output identity_name string = identity.name
@description('Generated id for the identity')
output identity_id string = identity.id
