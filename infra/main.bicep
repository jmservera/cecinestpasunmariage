param base_name string
param workspace_name string
param workspace_rg_name string
param location string = resourceGroup().location
param static_app_location string = location
param emailDataLocation string = 'switzerland'
param dns_zone_name string
param dnszone_rg_name string = resourceGroup().name
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
var databaseAccount_name = '${normalized_name}db'

module deployment_identity 'modules/user-assigned-identity.bicep' = {
  name: 'deployment_identity'
  params: {
    location: location
    tags: tags
  }
}

module acs_email './modules/acs.email.bicep' = {
  name: '${CommunicationServices_name}_email'
  params: {
    name: CommunicationServices_name
    emailDataLocation: emailDataLocation
    custom_domain_name: dns_zone_name
    tags: tags
  }
}

module domain_verification_entries 'modules/domain-verification-entries.bicep' = {
  name: '${CommunicationServices_name}_domainverification'
  scope: resourceGroup(dnszone_rg_name)
  params: {
    dnszone_name: dns_zone_name
    verificationRecords: acs_email.outputs.verificationRecords
  }
  dependsOn: [
    staticApp // do acs verification after the static app is created, txt will be overwritten
  ]
}

module domain_verification_start 'modules/domain-verification-start.bicep' = {
  name: '${CommunicationServices_name}_domain_verification_start'
  params: {
    dnszone_name: dns_zone_name
    identity_name: deployment_identity.outputs.identity_name
    acs_name: acs.outputs.name
    email_services_name: acs_email.outputs.name
    tags: tags
    emailDataLocation: emailDataLocation
  }
  dependsOn: [
    domain_verification_entries
  ]
}

module acs 'modules/acs.bicep' = {
  name: '${CommunicationServices_name}_acs'
  params: {
    name: CommunicationServices_name
    emailDataLocation: emailDataLocation
    tags: tags
  }
}

module cosmos 'modules/cosmos.bicep' = {
  name: databaseAccount_name
  params: {
    name: databaseAccount_name
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
    customDomain: dns_zone_name
    cosmosdb_name: cosmos.outputs.cosmosdb_resource_name
    functions_backend_id: functions.outputs.function_id
    functions_backend_location: functions.outputs.location
    resourcesLocation: location
    customDomain_rg_name: dnszone_rg_name
    identity_name: deployment_identity.outputs.identity_name
  }
}

module functions 'modules/functions.bicep' = {
  name: function_name
  params: {
    name: function_name
    location: location
    tags: tags
    cosmosdb_name: databaseAccount_name
    registrations_database_name: registrations_database_name
    user_registrations_container_name: user_registrations_container_name
    media_storage_account_name: storage.outputs.storageAccountName
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
    app_insights_rg_name: workspace_rg_name
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
