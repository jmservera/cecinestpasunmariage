param base_name string
param dnszones_name string
param workspace_name string
param location string = resourceGroup().location
param static_app_location string = location
param emailDataLocation string = 'switzerland'
param custom_domain_name string
param custom_domains array
param tags object = {
  env: 'prod'
}
param staticAppRepositoryUrl string
param telegram_token string

param vision_key string
param vision_endpoint string
param aoai_deployment_name string
param aoai_endpoint string
param aoai_key string
param computer_vision_key string
param computer_vision_endpoint string

param registrations_database_name string = 'registrations'
param user_registrations_container_name string = 'users'
param default_admin_email string

// keep the first 19 chars of the start_name
var short = substring(uniqueString(resourceGroup().id), 0, 4)
var truncated_name = length(base_name) > 11 ? substring(base_name, 0, 11) : base_name
var normalized_name = toLower('${truncated_name}${short}') // 15 chars max
var function_name = '${normalized_name}function'
var staticSites_name = '${normalized_name}site'
var media_storage_account_name = '${normalized_name}pictures' // 24 chars max
var CommunicationServices_name = '${normalized_name}acs'
var databaseAccounts_cecinestpasunmariagedb_name = '${normalized_name}db'

module acs_email './modules/acs.email.bicep' = {
  name: '${CommunicationServices_name}email'
  params: {
    name: CommunicationServices_name
    emailDataLocation: emailDataLocation
    custom_domain_name: custom_domain_name
    tags: tags
  }
}

module domain_verification 'modules/domain-verifications.bicep' = {
  name: '${CommunicationServices_name}domainverification'
  params: {
    dnszone_name: custom_domain_name
    verificationRecords: acs_email.outputs.verificationRecords
  }
}

module acs 'modules/acs.bicep' = {
  name: '${CommunicationServices_name}domainlink'
  params: {
    name: CommunicationServices_name
    emailDataLocation: emailDataLocation
    custom_domain_name: custom_domain_name
    tags: tags
  }
  // dependsOn: [
  //   domain_verification // not needed by now
  // ]
}

module cosmos 'modules/cosmos.bicep' = {
  name: databaseAccounts_cecinestpasunmariagedb_name
  params: {
    name: databaseAccounts_cecinestpasunmariagedb_name
    location: location
    tags: tags
    registrations_database_name: registrations_database_name
    user_registrations_container_name: user_registrations_container_name
  }
}

module staticApp 'modules/static-app.bicep' = {
  name: staticSites_name
  params: {
    name: staticSites_name
    location: static_app_location
    tags: tags
    repositoryUrl: staticAppRepositoryUrl
    customDomains: custom_domains
    cosmosdb_resource_id: cosmos.outputs.cosmosdb_resource_id
    functions_backend_id: functions.outputs.function_id
    functions_backend_location: functions.outputs.location
  }
}

module functions 'modules/functions.bicep' = {
  name: function_name
  params: {
    name: function_name
    location: location
    tags: tags
    cosmosdb_name: databaseAccounts_cecinestpasunmariagedb_name
    registrations_database_name: registrations_database_name
    user_registrations_container_name: user_registrations_container_name
    media_storage_account_name: media_storage_account_name
    telegram_token: telegram_token
    vision_key: vision_key
    vision_endpoint: vision_endpoint
    aoai_deployment_name: aoai_deployment_name
    aoai_endpoint: aoai_endpoint
    aoai_key: aoai_key
    computer_vision_key: computer_vision_key
    computer_vision_endpoint: computer_vision_endpoint
    acs_name: acs.outputs.name
    default_sender: acs_email.outputs.default_sender
    default_admin_email: default_admin_email
    app_insights_workspace_name: workspace_name
  }
}

module storage 'modules/site-storage.bicep' = {
  name: media_storage_account_name
  params: {
    name: media_storage_account_name
    location: location
    tags: tags
  }
}

resource dnszones_staticApp 'Microsoft.Network/dnszones@2023-07-01-preview' existing = {
  name: dnszones_name
}

resource Microsoft_Network_dnszones_A_staticApp 'Microsoft.Network/dnszones/A@2023-07-01-preview' = {
  parent: dnszones_staticApp
  name: '@'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticApp.outputs.staticSites_resource_id
    }
  }
}

resource Microsoft_Network_dnszones_CNAME_staticApp 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_staticApp
  name: '*'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticApp.outputs.staticSites_resource_id
    }
  }
}
