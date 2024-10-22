param customDomain string
param validationToken string

resource dnszonesStaticApp 'Microsoft.Network/dnszones@2023-07-01-preview' existing = {
  name: customDomain
}

resource dnsZonesStaticAppTXT 'Microsoft.Network/dnsZones/TXT@2023-07-01-preview' = if (validationToken != '') {
  parent: dnszonesStaticApp
  name: '@'
  properties: {
    TTL: 1
    TXTRecords: [
      {
        value: [validationToken]
      }
    ]
  }
}
