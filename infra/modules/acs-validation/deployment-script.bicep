param dns_zone_name string
param identity_name string
param location string = resourceGroup().location
param email_service_name string

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
  name: 'acs_domain_verification'
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
    arguments: '"${dns_zone_name}" "${resourceGroup().name}" "${email_service_name}"'
    cleanupPreference: 'OnExpiration'
    scriptContent: '''
      #!/bin/bash
      set -e
      az communication email domain initiate-verification --domain-name "$1" --resource-group "$2" --email-service-name "$3" --verification-type Domain
      az communication email domain initiate-verification --domain-name "$1" --resource-group "$2" --email-service-name "$3" --verification-type SPF
      az communication email domain initiate-verification --domain-name "$1" --resource-group "$2" --email-service-name "$3" --verification-type DKIM
      az communication email domain initiate-verification --domain-name "$1" --resource-group "$2" --email-service-name "$3" --verification-type DKIM2
    '''
  }
  dependsOn: [
    deployment_identity_configuration
  ]
}
