param name string
param tags object
param emailDataLocation string

resource acs_resource 'Microsoft.Communication/CommunicationServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: emailDataLocation
  }
}

output name string = acs_resource.name
