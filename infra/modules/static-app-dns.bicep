param customDomain string
param staticSite_id string

resource dnszones_staticApp 'Microsoft.Network/dnszones@2023-07-01-preview' existing = {
  name: customDomain
}

resource Microsoft_Network_dnszones_A_staticApp 'Microsoft.Network/dnszones/A@2023-07-01-preview' = {
  parent: dnszones_staticApp
  name: '@'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSite_id
    }
  }
}

resource Microsoft_Network_dnszones_CNAME_staticApp 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_staticApp
  name: '*'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSite_id
    }
  }
}
