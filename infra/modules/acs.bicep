param name string
param tags object
param emailDataLocation string
param custom_domain_name string

resource emailServices_acs_domain 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' existing = {
  name: custom_domain_name
}

resource acs_resource 'Microsoft.Communication/CommunicationServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: emailDataLocation
    // linkedDomains: [
    //   emailServices_acs_domain.id
    // ]
  }
}

output name string = acs_resource.name
