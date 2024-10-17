param name string
param location string
param resourcesLocation string = location
param repositoryUrl string
param customDomain string
param cosmosdb_name string
param functions_backend_id string
param functions_backend_location string
param tags object

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

resource cosmosdb_resource 'Microsoft.DocumentDB/databaseAccounts@2021-06-15' existing = {
  name: cosmosdb_name
}

resource dnszones_staticApp 'Microsoft.Network/dnszones@2023-07-01-preview' existing = {
  name: customDomain
}

resource Microsoft_Network_dnszones_A_staticApp 'Microsoft.Network/dnszones/A@2023-07-01-preview' = {
  parent: dnszones_staticApp
  name: '@'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSite.id
    }
  }
}

resource Microsoft_Network_dnszones_CNAME_staticApp 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_staticApp
  name: '*'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSite.id
    }
  }
}

//custom domains

resource staticSites_domains 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSite
  name: customDomain
  dependsOn: [
    Microsoft_Network_dnszones_CNAME_staticApp
  ]
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
