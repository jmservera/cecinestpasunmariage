param base_name string
param dnszones_name string
param b2cDirectories_name string
param workspace_name string
param location string = resourceGroup().location
param emailDataLocation string = 'switzerland'
param custom_domain_name string
param tags object = {
  env: 'prod'
}

// keep the first 19 chars of the start_name
var short = substring(uniqueString(resourceGroup().id), 0, 4)
var normalized_name = '${substring(base_name, 0, 10)}-${short}'
var function_name = '${normalized_name}efonction'
var logAnalytics_name = '${function_name}-loganalytics'
var staticSites_name = '${normalized_name}mariage'
var storageAccounts_pictures_name = '${normalized_name}pictures'
var CommunicationServices_name = '${normalized_name}acs'
var serverfarms_ASP_name = 'ASP-${staticSites_name}-a456' //todo generate unique 4 digit id
var storageAccounts_cecinestpasunmariagab6f_name = '${normalized_name}ab6f'
var databaseAccounts_cecinestpasunmariagedb_name = '${normalized_name}db'
var actionGroups_Application_Insights_Smart_Detection_name = 'Application Insights Smart Detection'
var smartdetectoralertrules_failure_anomalies_cecinestpasunefonction_name = 'failure anomalies - ${function_name}'

resource workspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  name: workspace_name
}

resource b2cDirectories_servezas_onmicrosoft_com_name_resource 'Microsoft.AzureActiveDirectory/b2cDirectories@2023-05-17-preview' existing = {
  name: b2cDirectories_name
}

module acs './modules/acs.bicep' = {
  name: CommunicationServices_name
  params: {
    name: CommunicationServices_name
    emailDataLocation: emailDataLocation
    custom_domain_name: custom_domain_name
    tags: tags
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_resource 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: databaseAccounts_cecinestpasunmariagedb_name
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'None'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: false
    enableAnalyticalStorage: false
    analyticalStorageConfiguration: {
      schemaType: 'WellDefined'
    }
    createMode: 'Default'
    databaseAccountOfferType: 'Standard'
    defaultIdentity: 'FirstPartyIdentity'
    networkAclBypass: 'None'
    disableLocalAuth: false
    enablePartitionMerge: false
    enableBurstCapacity: false
    minimalTlsVersion: 'Tls12'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: 'West Europe'
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    cors: [
      {
        allowedOrigins: 'http://localhost:4280, http://localhost:1313'
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    ipRules: []
    backupPolicy: {
      type: 'Continuous'
      continuousModeProperties: {
        tier: 'Continuous30Days'
      }
    }
    networkAclBypassResourceIds: []
    capacity: {
      totalThroughputLimit: 4000
    }
  }
}

resource actionGroups_Application_Insights_Smart_Detection_name_resource 'microsoft.insights/actionGroups@2023-09-01-preview' = {
  name: actionGroups_Application_Insights_Smart_Detection_name
  location: 'Global'
  properties: {
    groupShortName: 'SmartDetect'
    enabled: true
    emailReceivers: []
    smsReceivers: []
    webhookReceivers: []
    eventHubReceivers: []
    itsmReceivers: []
    azureAppPushReceivers: []
    automationRunbookReceivers: []
    voiceReceivers: []
    logicAppReceivers: []
    azureFunctionReceivers: []
    armRoleReceivers: [
      {
        name: 'Monitoring Contributor'
        roleId: '749f88d5-cbae-40b8-bcfc-e573ddc772fa'
        useCommonAlertSchema: true
      }
      {
        name: 'Monitoring Reader'
        roleId: '43d0d8ad-25c7-4714-9337-8ba259a9fe05'
        useCommonAlertSchema: true
      }
    ]
  }
}

resource components_cecinestpasunefonction_name_resource 'microsoft.insights/components@2020-02-02' = {
  name: logAnalytics_name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'IbizaWebAppExtensionCreate'
    RetentionInDays: 90
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource dnszones_cecinestpasunmariage_org_name_resource 'Microsoft.Network/dnszones@2023-07-01-preview' = {
  name: dnszones_name
  location: 'global'
  properties: {
    zoneType: 'Public'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_resource 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccounts_cecinestpasunmariagab6f_name
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
  properties: {
    defaultToOAuthAuthentication: true
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource storageAccounts_noweddingpictures_name_resource 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccounts_pictures_name
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    dnsEndpointType: 'Standard'
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
    allowSharedKeyAccess: true
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      requireInfrastructureEncryption: false
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource serverfarms_ASP_cecinestpasunmariage_a456_name_resource 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: serverfarms_ASP_name
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
    capacity: 0
  }
  kind: 'functionapp'
  properties: {
    perSiteScaling: false
    elasticScaleEnabled: false
    maximumElasticWorkerCount: 1
    isSpot: false
    reserved: true
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

resource staticSites_cecinestpasunmariage_name_resource 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticSites_name
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    repositoryUrl: 'https://github.com/jmservera/${staticSites_name}'
    branch: 'main'
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_Audits 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_resource
  name: 'Audits'
  properties: {
    resource: {
      id: 'Audits'
    }
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_registrations 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_resource
  name: 'registrations'
  properties: {
    resource: {
      id: 'registrations'
    }
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_00000000_0000_0000_0000_000000000001 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_resource
  name: '00000000-0000-0000-0000-000000000001'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_cecinestpasunmariagedb_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_00000000_0000_0000_0000_000000000002 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_resource
  name: '00000000-0000-0000-0000-000000000002'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_cecinestpasunmariagedb_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource components_cecinestpasunefonction_name_degradationindependencyduration 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'degradationindependencyduration'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'degradationindependencyduration'
      DisplayName: 'Degradation in dependency duration'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_degradationinserverresponsetime 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'degradationinserverresponsetime'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'degradationinserverresponsetime'
      DisplayName: 'Degradation in server response time'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_digestMailConfiguration 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'digestMailConfiguration'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'digestMailConfiguration'
      DisplayName: 'Digest Mail Configuration'
      Description: 'This rule describes the digest mail preferences'
      HelpUrl: 'www.homail.com'
      IsHidden: true
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_extension_billingdatavolumedailyspikeextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'extension_billingdatavolumedailyspikeextension'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'extension_billingdatavolumedailyspikeextension'
      DisplayName: 'Abnormal rise in daily data volume (preview)'
      Description: 'This detection rule automatically analyzes the billing data generated by your application, and can warn you about an unusual increase in your application\'s billing costs'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/tree/master/SmartDetection/billing-data-volume-daily-spike.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_extension_canaryextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'extension_canaryextension'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'extension_canaryextension'
      DisplayName: 'Canary extension'
      Description: 'Canary extension'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/'
      IsHidden: true
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_extension_exceptionchangeextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'extension_exceptionchangeextension'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'extension_exceptionchangeextension'
      DisplayName: 'Abnormal rise in exception volume (preview)'
      Description: 'This detection rule automatically analyzes the exceptions thrown in your application, and can warn you about unusual patterns in your exception telemetry.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/abnormal-rise-in-exception-volume.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_extension_memoryleakextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'extension_memoryleakextension'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'extension_memoryleakextension'
      DisplayName: 'Potential memory leak detected (preview)'
      Description: 'This detection rule automatically analyzes the memory consumption of each process in your application, and can warn you about potential memory leaks or increased memory consumption.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/tree/master/SmartDetection/memory-leak.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_extension_securityextensionspackage 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'extension_securityextensionspackage'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'extension_securityextensionspackage'
      DisplayName: 'Potential security issue detected (preview)'
      Description: 'This detection rule automatically analyzes the telemetry generated by your application and detects potential security issues.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/application-security-detection-pack.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_extension_traceseveritydetector 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'extension_traceseveritydetector'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'extension_traceseveritydetector'
      DisplayName: 'Degradation in trace severity ratio (preview)'
      Description: 'This detection rule automatically analyzes the trace logs emitted from your application, and can warn you about unusual patterns in the severity of your trace telemetry.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/degradation-in-trace-severity-ratio.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_longdependencyduration 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'longdependencyduration'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'longdependencyduration'
      DisplayName: 'Long dependency duration'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_migrationToAlertRulesCompleted 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'migrationToAlertRulesCompleted'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'migrationToAlertRulesCompleted'
      DisplayName: 'Migration To Alert Rules Completed'
      Description: 'A configuration that controls the migration state of Smart Detection to Smart Alerts'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: true
      IsEnabledByDefault: false
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    Enabled: false
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_slowpageloadtime 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'slowpageloadtime'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'slowpageloadtime'
      DisplayName: 'Slow page load time'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource components_cecinestpasunefonction_name_slowserverresponsetime 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_cecinestpasunefonction_name_resource
  name: 'slowserverresponsetime'
  location: location
  properties: {
    RuleDefinitions: {
      Name: 'slowserverresponsetime'
      DisplayName: 'Slow server response time'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: true
    CustomEmails: []
  }
}

resource Microsoft_Network_dnszones_NS_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/NS@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: '@'
  properties: {
    TTL: 172800
    NSRecords: [
      {
        nsdname: 'ns1-32.azure-dns.com.'
      }
      {
        nsdname: 'ns2-32.azure-dns.net.'
      }
      {
        nsdname: 'ns3-32.azure-dns.org.'
      }
      {
        nsdname: 'ns4-32.azure-dns.info.'
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource dnszones_cecinestpasunmariage_org_name_dev 'Microsoft.Network/dnszones/NS@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: 'dev'
  properties: {
    TTL: 3600
    NSRecords: [
      {
        nsdname: 'ns1-06.azure-dns.com.'
      }
      {
        nsdname: 'ns2-06.azure-dns.net.'
      }
      {
        nsdname: 'ns3-06.azure-dns.org.'
      }
      {
        nsdname: 'ns4-06.azure-dns.info.'
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_SOA_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/SOA@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: '@'
  properties: {
    TTL: 3600
    SOARecord: {
      email: 'azuredns-hostmaster.microsoft.com'
      expireTime: 2419200
      host: 'ns1-32.azure-dns.com.'
      minimumTTL: 300
      refreshTime: 3600
      retryTime: 300
      serialNumber: 1
    }
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_TXT_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: '@'
  properties: {
    TTL: 3600
    TXTRecords: [
      {
        value: [
          '17sjpqsqxqf08knbq50dqs76qr1spclv'
        ]
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_TXT_dnszones_cecinestpasunmariage_org_name_dev 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: 'dev'
  properties: {
    TTL: 3600
    TXTRecords: [
      {
        value: [
          'ms-domain-verification=0c142b2e-2a05-42e1-a2f2-2f6524ecf26d'
        ]
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_resource
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      allowPermanentDelete: false
      enabled: false
    }
  }
}

resource storageAccounts_noweddingpictures_name_default 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_resource
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      allowPermanentDelete: false
      enabled: true
      days: 7
    }
    isVersioningEnabled: false
    changeFeed: {
      enabled: false
    }
    restorePolicy: {
      enabled: false
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource Microsoft_Storage_storageAccounts_fileServices_storageAccounts_cecinestpasunmariagab6f_name_default 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_resource
  name: 'default'
  properties: {
    protocolSettings: {
      smb: {}
    }
    cors: {
      corsRules: []
    }
    shareDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource Microsoft_Storage_storageAccounts_fileServices_storageAccounts_noweddingpictures_name_default 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_resource
  name: 'default'
  properties: {
    protocolSettings: {
      smb: {}
    }
    cors: {
      corsRules: []
    }
    shareDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource Microsoft_Storage_storageAccounts_queueServices_storageAccounts_cecinestpasunmariagab6f_name_default 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_resource
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
  }
}

resource Microsoft_Storage_storageAccounts_queueServices_storageAccounts_noweddingpictures_name_default 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_resource
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
  }
}

resource Microsoft_Storage_storageAccounts_tableServices_storageAccounts_cecinestpasunmariagab6f_name_default 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_resource
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
  }
}

resource Microsoft_Storage_storageAccounts_tableServices_storageAccounts_noweddingpictures_name_default 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_resource
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
  }
}

resource sites_cecinestpasunefonction_name_resource 'Microsoft.Web/sites@2023-12-01' = {
  name: function_name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  properties: {
    enabled: true
    hostNameSslStates: [
      {
        name: '${function_name}.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
      {
        name: '${function_name}.scm.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Repository'
      }
    ]
    serverFarmId: serverfarms_ASP_cecinestpasunmariage_a456_name_resource.id
    reserved: true
    isXenon: false
    hyperV: false
    dnsConfiguration: {}
    vnetRouteAllEnabled: false
    vnetImagePullEnabled: false
    vnetContentShareEnabled: false
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      acrUseManagedIdentityCreds: false
      alwaysOn: false
      http20Enabled: false
      functionAppScaleLimit: 200
      minimumElasticInstanceCount: 1
    }
    scmSiteAlsoStopped: false
    clientAffinityEnabled: false
    clientCertEnabled: false
    clientCertMode: 'Required'
    hostNamesDisabled: false
    vnetBackupRestoreEnabled: false
    customDomainVerificationId: '38B90B5236058CFA4C21579FFD4FDE35848F30DD5BBE17FF1728802693D6E08A'
    containerSize: 1536
    dailyMemoryTimeQuota: 0
    httpsOnly: true
    redundancyMode: 'None'
    publicNetworkAccess: 'Enabled'
    storageAccountRequired: false
    keyVaultReferenceIdentity: 'SystemAssigned'
  }
}

resource sites_cecinestpasunefonction_name_ftp 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'ftp'
  properties: {
    allow: true
  }
}

resource sites_cecinestpasunefonction_name_scm 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'scm'
  properties: {
    allow: true
  }
}

resource sites_cecinestpasunefonction_name_web 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'web'
  properties: {
    numberOfWorkers: 1
    defaultDocuments: [
      'Default.htm'
      'Default.html'
      'Default.asp'
      'index.htm'
      'index.html'
      'iisstart.htm'
      'default.aspx'
      'index.php'
    ]
    netFrameworkVersion: 'v4.0'
    linuxFxVersion: 'DOTNET-ISOLATED|8.0'
    requestTracingEnabled: false
    remoteDebuggingEnabled: false
    httpLoggingEnabled: false
    acrUseManagedIdentityCreds: false
    logsDirectorySizeLimit: 35
    detailedErrorLoggingEnabled: false
    publishingUsername: '$cecinestpasunefonction'
    scmType: 'GitHubAction'
    use32BitWorkerProcess: false
    webSocketsEnabled: false
    alwaysOn: false
    managedPipelineMode: 'Integrated'
    virtualApplications: [
      {
        virtualPath: '/'
        physicalPath: 'site\\wwwroot'
        preloadEnabled: false
      }
    ]
    loadBalancing: 'LeastRequests'
    experiments: {
      rampUpRules: []
    }
    autoHealEnabled: false
    vnetRouteAllEnabled: false
    vnetPrivatePortsCount: 0
    publicNetworkAccess: 'Enabled'
    cors: {
      allowedOrigins: [
        'https://portal.azure.com'
        'https://cecinestpasunmariage.org'
      ]
      supportCredentials: false
    }
    localMySqlEnabled: false
    ipSecurityRestrictions: [
      {
        ipAddress: 'Any'
        action: 'Allow'
        priority: 2147483647
        name: 'Allow all'
        description: 'Allow all access'
      }
    ]
    scmIpSecurityRestrictions: [
      {
        ipAddress: 'Any'
        action: 'Allow'
        priority: 2147483647
        name: 'Allow all'
        description: 'Allow all access'
      }
    ]
    scmIpSecurityRestrictionsUseMain: false
    http20Enabled: false
    minTlsVersion: '1.2'
    scmMinTlsVersion: '1.2'
    ftpsState: 'FtpsOnly'
    preWarmedInstanceCount: 0
    functionAppScaleLimit: 200
    functionsRuntimeScaleMonitoringEnabled: false
    minimumElasticInstanceCount: 1
    azureStorageAccounts: {}
  }
}

resource sites_cecinestpasunefonction_name_760bbd36_d292_42a7_adea_de636fe70e5c 'Microsoft.Web/sites/deployments@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: '760bbd36-d292-42a7-adea-de636fe70e5c'
  properties: {
    status: 4
    author_email: 'N/A'
    author: 'N/A'
    deployer: 'GITHUB_ZIP_DEPLOY_FUNCTIONS_V1'
    message: '{"type":"deployment","sha":"48d22ac738d699e89e26f45c3fee850d9b6e86b3","repoName":"jmservera/cecinestpasunmariage","actor":"jmservera","slotName":"production"}'
    start_time: '2024-10-16T08:29:57.0155926Z'
    end_time: '2024-10-16T08:30:13.6641118Z'
    active: true
  }
}

resource sites_cecinestpasunefonction_name_Cecinestpasunbot 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'Cecinestpasunbot'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/Cecinestpasunbot.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/Cecinestpasunbot'
    config: {
      name: 'Cecinestpasunbot'
      entryPoint: 'functions.Cecinestpasunbot.Update'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'req'
          type: 'httpTrigger'
          direction: 'In'
          authLevel: 'Anonymous'
          methods: [
            'get'
            'post'
          ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'Out'
        }
      ]
    }
    invoke_url_template: 'https://cecinestpasunefonction.azurewebsites.net/api/cecinestpasunbot'
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_Cecinestpasunbotreg 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'Cecinestpasunbotreg'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/Cecinestpasunbotreg.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/Cecinestpasunbotreg'
    config: {
      name: 'Cecinestpasunbotreg'
      entryPoint: 'functions.Cecinestpasunbot.Register'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'req'
          type: 'httpTrigger'
          direction: 'In'
          authLevel: 'Anonymous'
          methods: [
            'get'
            'post'
          ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'Out'
        }
      ]
    }
    invoke_url_template: 'https://cecinestpasunefonction.azurewebsites.net/api/cecinestpasunbotreg'
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_CosmosChanges 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'CosmosChanges'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/CosmosChanges.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/CosmosChanges'
    config: {
      name: 'CosmosChanges'
      entryPoint: 'functions.CosmosChanges.Run'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'items'
          type: 'cosmosDBTrigger'
          direction: 'In'
          databaseName: '%DATABASE_NAME%'
          containerName: '%DATABASE_CONTAINER_NAME%'
          connection: 'DATABASE_CONNECTION_STRING'
          leaseContainerName: 'leases'
          createLeaseContainerIfNotExists: true
        }
      ]
    }
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_GetPhotos 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'GetPhotos'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/GetPhotos.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/GetPhotos'
    config: {
      name: 'GetPhotos'
      entryPoint: 'functions.GetPhotos.Run'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'req'
          type: 'httpTrigger'
          direction: 'In'
          authLevel: 'Anonymous'
          methods: [
            'get'
            'post'
          ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'Out'
        }
      ]
    }
    invoke_url_template: 'https://cecinestpasunefonction.azurewebsites.net/api/getphotos'
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_PictureDescriber 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'PictureDescriber'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/PictureDescriber.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/PictureDescriber'
    config: {
      name: 'PictureDescriber'
      entryPoint: 'functions.PictureDescriber.Run'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'client'
          type: 'blobTrigger'
          direction: 'In'
          properties: {
            supportsDeferredBinding: 'True'
          }
          path: 'pics/{name}'
          connection: ''
        }
      ]
    }
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_SendEmail 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'SendEmail'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/SendEmail.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/SendEmail'
    config: {
      name: 'SendEmail'
      entryPoint: 'functions.SendEmail.Run'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'req'
          type: 'httpTrigger'
          direction: 'In'
          authLevel: 'Anonymous'
          methods: [
            'post'
          ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'Out'
        }
      ]
    }
    invoke_url_template: 'https://cecinestpasunefonction.azurewebsites.net/api/sendemail'
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_Upload 'Microsoft.Web/sites/functions@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: 'Upload'
  properties: {
    script_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/home/site/wwwroot/functions.dll'
    test_data_href: 'https://cecinestpasunefonction.azurewebsites.net/admin/vfs/tmp/FunctionsData/Upload.dat'
    href: 'https://cecinestpasunefonction.azurewebsites.net/admin/functions/Upload'
    config: {
      name: 'Upload'
      entryPoint: 'functions.Upload.Run'
      scriptFile: 'functions.dll'
      language: 'dotnet-isolated'
      functionDirectory: ''
      bindings: [
        {
          name: 'req'
          type: 'httpTrigger'
          direction: 'In'
          authLevel: 'Anonymous'
          methods: [
            'post'
          ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'Out'
        }
      ]
    }
    invoke_url_template: 'https://cecinestpasunefonction.azurewebsites.net/api/upload'
    language: 'dotnet-isolated'
    isDisabled: false
  }
}

resource sites_cecinestpasunefonction_name_sites_cecinestpasunefonction_name_azurewebsites_net 'Microsoft.Web/sites/hostNameBindings@2023-12-01' = {
  parent: sites_cecinestpasunefonction_name_resource
  name: '${function_name}.azurewebsites.net'
  properties: {
    siteName: 'cecinestpasunefonction'
    hostNameType: 'Verified'
  }
}

resource staticSites_cecinestpasunmariage_name_default 'Microsoft.Web/staticSites/basicAuth@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'default'
  properties: {
    applicableEnvironmentsMode: 'SpecifiedEnvironments'
  }
}

resource staticSites_cecinestpasunmariage_name_staticSites_cecinestpasunmariage_name_org 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: '${staticSites_name}.org'
}

resource staticSites_cecinestpasunmariage_name_lanobodadeisayjuanma_com 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'lanobodadeisayjuanma.com'
  properties: {}
}

resource staticSites_cecinestpasunmariage_name_lanobodadeisayjuanma_es 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'lanobodadeisayjuanma.es'
  properties: {}
}

resource staticSites_cecinestpasunmariage_name_www_staticSites_cecinestpasunmariage_name_org 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'www.${staticSites_name}.org'
  properties: {}
}

resource staticSites_cecinestpasunmariage_name_www_lanobodadeisayjuanma_com 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'www.lanobodadeisayjuanma.com'
  properties: {}
}

resource smartdetectoralertrules_failure_anomalies_cecinestpasunefonction_name_resource 'microsoft.alertsmanagement/smartdetectoralertrules@2021-04-01' = {
  name: smartdetectoralertrules_failure_anomalies_cecinestpasunefonction_name
  location: 'global'
  properties: {
    description: 'Failure Anomalies notifies you of an unusual rise in the rate of failed HTTP requests or dependency calls.'
    state: 'Enabled'
    severity: 'Sev3'
    frequency: 'PT1M'
    detector: {
      id: 'FailureAnomaliesDetector'
    }
    scope: [
      components_cecinestpasunefonction_name_resource.id
    ]
    actionGroups: {
      groupIds: [
        actionGroups_Application_Insights_Smart_Detection_name_resource.id
      ]
    }
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_Audits_AuditLogs 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_Audits
  name: 'AuditLogs'
  properties: {
    resource: {
      id: 'AuditLogs'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/user'
        ]
        kind: 'Hash'
      }
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
    }
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_registrations_leases 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_registrations
  name: 'leases'
  properties: {
    resource: {
      id: 'leases'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
      defaultTtl: -1
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
    }
  }
}

resource databaseAccounts_cecinestpasunmariagedb_name_registrations_users 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: databaseAccounts_cecinestpasunmariagedb_name_registrations
  name: 'users'
  properties: {
    resource: {
      id: 'users'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
        version: 2
      }
      uniqueKeyPolicy: {
        uniqueKeys: []
      }
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
      computedProperties: []
    }
  }
}

resource Microsoft_Network_dnszones_A_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/A@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: '@'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSites_cecinestpasunmariage_name_resource.id
    }
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_CNAME_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_cecinestpasunmariage_org_name_resource
  name: '*'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSites_cecinestpasunmariage_name_resource.id
    }
    trafficManagementProfile: {}
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_azure_webjobs_hosts 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'azure-webjobs-hosts'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_noweddingpictures_name_default_azure_webjobs_hosts 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_default
  name: 'azure-webjobs-hosts'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_azure_webjobs_secrets 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'azure-webjobs-secrets'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_noweddingpictures_name_default_azure_webjobs_secrets 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_default
  name: 'azure-webjobs-secrets'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_noweddingpictures_name_default_chat_history 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_default
  name: 'chat-history'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_function_releases 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'function-releases'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_insights_logs_storagewrite 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'insights-logs-storagewrite'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_insights_metrics_pt1m 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'insights-metrics-pt1m'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_noweddingpictures_name_default_pics 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_default
  name: 'pics'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_scm_releases 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'scm-releases'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_noweddingpictures_name_default_scm_releases 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_default
  name: 'scm-releases'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_noweddingpictures_name_default_thumbnails 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageAccounts_noweddingpictures_name_default
  name: 'thumbnails'
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_cecinestpasunefonction82ad 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: Microsoft_Storage_storageAccounts_fileServices_storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'cecinestpasunefonction82ad'
  properties: {
    accessTier: 'TransactionOptimized'
    shareQuota: 102400
    enabledProtocols: 'SMB'
  }
}

resource storageAccounts_noweddingpictures_name_default_azure_webjobs_blobtrigger_46e0c7d7ebb2_1769703605 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: Microsoft_Storage_storageAccounts_queueServices_storageAccounts_noweddingpictures_name_default
  name: 'azure-webjobs-blobtrigger-46e0c7d7ebb2-1769703605'
  properties: {
    metadata: {}
  }
}

resource storageAccounts_noweddingpictures_name_default_azure_webjobs_blobtrigger_cecinestpasunefonction 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: Microsoft_Storage_storageAccounts_queueServices_storageAccounts_noweddingpictures_name_default
  name: 'azure-webjobs-blobtrigger-cecinestpasunefonction'
  properties: {
    metadata: {}
  }
}

resource storageAccounts_noweddingpictures_name_default_webjobs_blobtrigger_poison 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: Microsoft_Storage_storageAccounts_queueServices_storageAccounts_noweddingpictures_name_default
  name: 'webjobs-blobtrigger-poison'
  properties: {
    metadata: {}
  }
}

resource storageAccounts_cecinestpasunmariagab6f_name_default_AzureFunctionsDiagnosticEvents202401 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: Microsoft_Storage_storageAccounts_tableServices_storageAccounts_cecinestpasunmariagab6f_name_default
  name: 'AzureFunctionsDiagnosticEvents202401'
  properties: {}
}

resource Microsoft_Web_staticSites_databaseConnections_staticSites_cecinestpasunmariage_name_default 'Microsoft.Web/staticSites/databaseConnections@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'default'
  properties: {
    resourceId: databaseAccounts_cecinestpasunmariagedb_name_resource.id
    region: 'West Europe'
  }
}

resource staticSites_cecinestpasunmariage_name_backend1 'Microsoft.Web/staticSites/linkedBackends@2023-12-01' = {
  parent: staticSites_cecinestpasunmariage_name_resource
  name: 'backend1'
  properties: {
    backendResourceId: sites_cecinestpasunefonction_name_resource.id
    region: sites_cecinestpasunefonction_name_resource.location
  }
}
