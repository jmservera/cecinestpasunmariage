param name string
param emailDataLocation string
param custom_domain_name string
param tags object
param senderusernames array = [
  {
    name: 'donotreply'
    username: 'DoNotReply'
    displayName: 'DoNotReply'
  }
  {
    name: 'hello'
    username: 'hello'
    displayName: 'About us'
  }
  {
    name: 'me'
    username: 'me'
    displayName: 'Myself'
  }
]

var emailServices_name = '${name}-emailacs'

resource emailServices_acs_resource 'Microsoft.Communication/emailServices@2023-06-01-preview' = {
  name: emailServices_name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: emailDataLocation
  }
}

resource emailServices_acs_domain 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' = {
  parent: emailServices_acs_resource
  name: custom_domain_name
  location: 'global'
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource emailServices_acs_domain_senderUsernames 'microsoft.communication/emailservices/domains/senderusernames@2023-06-01-preview' = [
  for sender in senderusernames: {
    parent: emailServices_acs_domain
    name: sender.name
    properties: {
      username: sender.username
      displayName: sender.displayName
    }
  }
]

output name string = emailServices_acs_resource.name
output default_sender string = '${senderusernames[0].username}@${custom_domain_name}'
output verificationRecords object = emailServices_acs_domain.properties.verificationRecords
