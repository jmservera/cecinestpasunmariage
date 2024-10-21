param name string
param location string
param tags object
// param storageAccounts_pictures_name string
// param acs_name string
param cosmosdb_name string
param registrations_database_name string
param user_registrations_container_name string
param media_storage_account_name string
param telegram_token string
param vision_key string
param vision_endpoint string
param aoai_deployment_name string
param aoai_endpoint string
param aoai_key string
param computer_vision_key string
param computer_vision_endpoint string
param acs_name string
param default_sender string
param default_admin_email string
param app_insights_workspace_name string
param app_insights_rg_name string

var storageAccountName = '${uniqueString(resourceGroup().id)}azfunctions'
var hostingPlan_name = 'ASP-${name}-a456' //todo generate unique 4 digit id
var media_storage_connectionString = 'DefaultEndpointsProtocol=https;AccountName=${media_storage_resource.name};AccountKey=${media_storage_resource.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource cosmosdb_resource 'Microsoft.DocumentDB/databaseAccounts@2021-06-15' existing = {
  name: cosmosdb_name
}

resource media_storage_resource 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: media_storage_account_name
}

resource acs_resource 'Microsoft.Communication/communicationServices@2023-06-01-preview' existing = {
  name: acs_name
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  name: app_insights_workspace_name
  scope: resourceGroup(app_insights_rg_name)
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

var storageAccount_connectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlan_name
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
    capacity: 0
  }
  properties: {
    reserved: true
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: workspace.id
  }
}

resource sites_function_resource 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: hostingPlan.id
    reserved: true //needed for functionapp,linux
    siteConfig: {
      netFrameworkVersion: 'v4.0'
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        //configuraion values for the general function configuration
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount_connectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccount_connectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(name)
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        // configuration values for the specific functions
        {
          name: 'DATABASE_CONNECTION_STRING'
          value: cosmosdb_resource.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'DATABASE_NAME'
          value: registrations_database_name
        }
        {
          name: 'DATABASE_CONTAINER_NAME'
          value: user_registrations_container_name
        }
        {
          name: 'STORAGE_CONNECTION_STRING'
          value: media_storage_connectionString
        }
        {
          name: 'TELEGRAM_TOKEN'
          value: telegram_token
        }
        {
          name: 'VISION_KEY'
          value: vision_key
        }
        {
          name: 'VISION_ENDPOINT'
          value: vision_endpoint
        }
        {
          name: 'AOAI_DEPLOYMENT_NAME'
          value: aoai_deployment_name
        }
        {
          name: 'AOAI_ENDPOINT'
          value: aoai_endpoint
        }
        {
          name: 'AOAI_KEY'
          value: aoai_key
        }
        {
          name: 'COMPUTER_VISION_KEY'
          value: computer_vision_key
        }
        {
          name: 'COMPUTER_VISION_ENDPOINT'
          value: computer_vision_endpoint
        }
        {
          name: 'COMMUNICATION_SERVICES_CONNECTION_STRING'
          value: acs_resource.listKeys().primaryConnectionString
        }
        {
          name: 'COMMUNICATION_SERVICES_SENDER'
          value: default_sender
        }
        {
          name: 'DEFAULT_ADMIN_EMAIL'
          value: default_admin_email
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

output function_id string = sites_function_resource.id
output function_name string = sites_function_resource.name
output location string = sites_function_resource.location
