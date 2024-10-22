// import { DomainPropertiesVerificationRecords } from 'Microsoft.Communication/emailServices/domains@2023-06-01-preview'
param dnszone_name string
param email_services_name string
param identity_name string
param acs_name string
param tags object
param emailDataLocation string

resource emailServices_acs_resource 'Microsoft.Communication/emailServices@2023-06-01-preview' existing = {
  name: email_services_name
}

resource emailServices_acs_domain 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' existing = {
  name: dnszone_name
  parent: emailServices_acs_resource
}

module acs_validation 'acs-validation/deployment-script.bicep' = {
  name: 'acs_validation'
  params: {
    identityName: identity_name
    dnsZoneName: dnszone_name
    emailServiceName: email_services_name
  }
}

resource acs_resource_add_email_services 'Microsoft.Communication/CommunicationServices@2023-06-01-preview' = {
  name: acs_name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: emailDataLocation
    linkedDomains: [
      emailServices_acs_domain.id
    ]
  }
  dependsOn: [
    acs_validation
  ]
}
