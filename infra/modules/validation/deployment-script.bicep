param dns_zone_name string
param identity_id string
param location string = resourceGroup().location
param static_webapp_name string

// https://github.com/TheCloudWarrior/AzureStaticWebApp/blob/main/_modules/deploymentscript/main.bicep

resource deploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'domain_verification'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity_id}': {}
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
      az staticwebapp hostname show --name "$1" --resource-group "$2" --hostname "$3" > $AZ_SCRIPTS_OUTPUT_PATH
    '''
  }
}

output validationToken string = deploymentScript.properties.outputs.validationToken
