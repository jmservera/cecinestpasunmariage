param dnsZoneName string
param identityName string
param location string = resourceGroup().location
param staticWebappName string

// https://github.com/TheCloudWarrior/AzureStaticWebApp/blob/main/_modules/deploymentscript/main.bicep

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
}

// this calls the module where we configure the identity permissions
module deploymentIdentityConfiguration 'deployment-identity-config.bicep' = {
  name: 'deployment_identity_configuration'
  params: {
    identityName: identityName
  }
}

resource deploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'domain_verification'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }

  properties: {
    azCliVersion: '2.59.0'
    retentionInterval: 'PT1H'
    arguments: '"${staticWebappName}" "${resourceGroup().name}" "${dnsZoneName}"'
    cleanupPreference: 'OnExpiration'
    scriptContent: '''
      #!/bin/bash
      set -e
      validationToken=$(az staticwebapp hostname show --name "$1" --resource-group "$2" --hostname "$3" --query validationToken -o tsv)
      echo "{'validationToken':'$validationToken'}" > $AZ_SCRIPTS_OUTPUT_PATH
    '''
  }
  dependsOn: [
    deploymentIdentityConfiguration
  ]
}

output validationToken string = deploymentScript.properties.outputs.validationToken
