param name string
param tags object
param location string
param corsOrigins array = [
  'http://localhost:4280'
  'http://localhost:1313'
]
param registrations_database_name string = 'registrations'
param user_registrations_container_name string = 'users'

resource databaseAccounts_resource 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: name
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    publicNetworkAccess: 'Enabled'
    createMode: 'Default'
    databaseAccountOfferType: 'Standard'
    minimalTlsVersion: 'Tls12'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    cors: [
      {
        allowedOrigins: join(corsOrigins, ',')
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource databaseAccounts_Audits 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: databaseAccounts_resource
  name: 'Audits'
  properties: {
    resource: {
      id: 'Audits'
    }
  }
}

resource databaseAccounts_registrations 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: databaseAccounts_resource
  name: registrations_database_name
  properties: {
    resource: {
      id: registrations_database_name
    }
  }
}

// resource databaseAccounts_00000000_0000_0000_0000_000000000001 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
//   parent: databaseAccounts_resource
//   name: '00000000-0000-0000-0000-000000000001'
//   properties: {
//     roleName: 'Cosmos DB Built-in Data Reader'
//     type: 'BuiltInRole'
//     assignableScopes: [
//       databaseAccounts_resource.id
//     ]
//     permissions: [
//       {
//         dataActions: [
//           'Microsoft.DocumentDB/databaseAccounts/readMetadata'
//           'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
//           'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
//           'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
//         ]
//         notDataActions: []
//       }
//     ]
//   }
// }

// resource databaseAccounts_00000000_0000_0000_0000_000000000002 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
//   parent: databaseAccounts_resource
//   name: '00000000-0000-0000-0000-000000000002'
//   properties: {
//     roleName: 'Cosmos DB Built-in Data Contributor'
//     type: 'BuiltInRole'
//     assignableScopes: [
//       databaseAccounts_resource.id
//     ]
//     permissions: [
//       {
//         dataActions: [
//           'Microsoft.DocumentDB/databaseAccounts/readMetadata'
//           'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
//           'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
//         ]
//         notDataActions: []
//       }
//     ]
//   }
// }

// resource databaseAccounts_registrations_leases 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
//   parent: databaseAccounts_registrations
//   name: 'leases'
//   properties: {
//     resource: {
//       id: 'leases'
//       indexingPolicy: {
//         indexingMode: 'consistent'
//         automatic: true
//         includedPaths: [
//           {
//             path: '/*'
//           }
//         ]
//         excludedPaths: [
//           {
//             path: '/"_etag"/?'
//           }
//         ]
//       }
//       partitionKey: {
//         paths: [
//           '/id'
//         ]
//         kind: 'Hash'
//       }
//       defaultTtl: -1
//       conflictResolutionPolicy: {
//         mode: 'LastWriterWins'
//         conflictResolutionPath: '/_ts'
//       }
//     }
//   }
// }

resource databaseAccounts_registrations_users 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: databaseAccounts_registrations
  name: user_registrations_container_name
  properties: {
    resource: {
      id: user_registrations_container_name
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

resource databaseAccounts_Audits_AuditLogs 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: databaseAccounts_Audits
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

output cosmosdb_resource_id string = databaseAccounts_resource.id
output cosmosdb_resource_name string = databaseAccounts_resource.name
