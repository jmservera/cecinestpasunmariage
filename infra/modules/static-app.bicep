param name string
param location string
param resourcesLocation string = location
param repositoryUrl string
param customDomain string
param customDomain_rg_name string
param cosmosdb_name string
param functions_backend_id string
param functions_backend_location string
param tags object
param identity_name string

resource cosmosdb_resource 'Microsoft.DocumentDB/databaseAccounts@2021-06-15' existing = {
  name: cosmosdb_name
}

resource staticSite 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: 'main'
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

//custom domains
module staticSites_dns 'static-app-dns.bicep' = {
  name: 'staticSites_dns'
  scope: resourceGroup(customDomain_rg_name)
  params: {
    customDomain: customDomain
    staticSite_id: staticSite.id
  }
}

resource staticSites_domains_start 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSite
  name: customDomain
  properties: {
    validationMethod: 'dns-txt-token'
  }
  dependsOn: [
    staticSites_dns
  ]
}

module validate 'validation/deployment-script.bicep' = {
  name: 'domain_verification'
  params: {
    dns_zone_name: customDomain
    static_webapp_name: staticSite.name
    identity_name: identity_name
  }
  dependsOn: [
    staticSites_dns
  ]
}

module staticSites_txt 'static-app-dns-txt.bicep' = {
  name: 'staticSites_txt_start'
  scope: resourceGroup(customDomain_rg_name)
  params: {
    customDomain: customDomain
    validationToken: validate.outputs.validationToken
  }
}

// database and backend

resource staticSites_databaseConnections_default 'Microsoft.Web/staticSites/databaseConnections@2023-12-01' = {
  parent: staticSite
  name: 'default'
  properties: {
    resourceId: cosmosdb_resource.id
    region: resourcesLocation
    connectionString: cosmosdb_resource.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource staticSite_backend 'Microsoft.Web/staticSites/linkedBackends@2023-12-01' = {
  parent: staticSite
  name: 'functionsbackend'
  properties: {
    backendResourceId: functions_backend_id
    region: functions_backend_location
  }
}

output staticSites_resource_id string = staticSite.id
output staticSites_resource_name string = staticSite.name
