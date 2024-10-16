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

resource emailServices_ceciemailacs_name_resource 'Microsoft.Communication/emailServices@2023-06-01-preview' = {
  name: emailServices_name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: emailDataLocation
  }
}

resource CommunicationServices_ceciacs_name_resource 'Microsoft.Communication/CommunicationServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: emailDataLocation
    linkedDomains: [
      emailServices_ceciemailacs_name_servezas_org.id
    ]
  }
}

resource emailServices_ceciemailacs_name_servezas_org 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' = {
  parent: emailServices_ceciemailacs_name_resource
  name: custom_domain_name
  location: 'global'
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource emailServices_ceciemailacs_name_servezas_org_senderUsernames 'microsoft.communication/emailservices/domains/senderusernames@2023-06-01-preview' = [
  for sender in senderusernames: {
    parent: emailServices_ceciemailacs_name_servezas_org
    name: sender.name
    properties: {
      username: sender.username
      displayName: sender.displayName
    }
  }
]
