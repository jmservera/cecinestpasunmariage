param customDomain string
param validationToken string

resource dnszones_staticApp 'Microsoft.Network/dnszones@2023-07-01-preview' existing = {
  name: customDomain
}

resource Microsoft_Network_dnszones_A_staticApp 'Microsoft.Network/dnsZones/TXT@2023-07-01-preview' = if (validationToken != '') {
  parent: dnszones_staticApp
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
