/// https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deployment-script-bicep?tabs=CLI
param identity_name string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identity_name
}

// Creates the custom role to access the actions needed for the deployment script
// Using least privilege principle
resource customRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' = {
  name: guid(
    'deployment-script-minimum-privilege-for-deployment-principal-acs',
    identity_name,
    subscription().subscriptionId,
    resourceGroup().name
  )
  scope: resourceGroup()
  properties: {
    roleName: '${resourceGroup().name}-deployment-script-minimum-privilege-for-deployment-principal-acs'
    description: 'Configure least privilege for the deployment principal in deployment script'
    type: 'customRole'
    permissions: [
      {
        actions: [
          'Microsoft.Communication/EmailServices/Domains/InitiateVerification/action'
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
    'deployment-script-minimum-privilege-for-deployment-principal-acs',
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
