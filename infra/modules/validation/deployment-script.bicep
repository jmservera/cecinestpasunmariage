param dns_zone_name string
param identity_name string
param location string = resourceGroup().location
param static_webapp_name string

// https://github.com/TheCloudWarrior/AzureStaticWebApp/blob/main/_modules/deploymentscript/main.bicep

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identity_name
}

module deployment_identity_configuration 'deployment-identity-config.bicep' = {
  name: 'deployment_identity_configuration'
  params: {
    identity_name: identity_name
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
    arguments: '"${static_webapp_name}" "${resourceGroup().name}" "${dns_zone_name}"'
    cleanupPreference: 'OnExpiration'
    scriptContent: '''
      #!/bin/bash
      set -e
      validationToken=$(az staticwebapp hostname show --name "$1" --resource-group "$2" --hostname "$3" --query validationToken -o tsv)
      echo "{'validationToken':'$validationToken'}" > $AZ_SCRIPTS_OUTPUT_PATH
    '''
  }
  dependsOn: [
    deployment_identity_configuration
  ]
}

output validationToken string = deploymentScript.properties.outputs.validationToken
