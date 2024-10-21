// import { DomainPropertiesVerificationRecords } from 'Microsoft.Communication/emailServices/domains@2023-06-01-preview'
param dnszone_name string
param verificationRecords object

resource dnszone 'Microsoft.Network/dnsZones@2023-07-01-preview' existing = {
  name: dnszone_name
}

resource Microsoft_Network_dnszones_TXT_domain_spf 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
  parent: dnszone
  name: '@'
  properties: {
    TTL: verificationRecords.Domain.ttl
    TXTRecords: [
      {
        value: [
          '${verificationRecords.Domain.value}'
        ]
      }
      { value: ['${verificationRecords.SPF.value}'] }
    ]
  }
}

resource Microsoft_Network_dnszones_DKIM 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszone
  name: verificationRecords.DKIM.name
  properties: {
    TTL: verificationRecords.DKIM.ttl
    CNAMERecord: {
      cname: '${verificationRecords.DKIM.value}'
    }
  }
}

resource Microsoft_Network_dnszones_DKIM2 'Microsoft.Network/dnsZones/CNAME@2023-07-01-preview' = {
  parent: dnszone
  name: verificationRecords.DKIM2.name
  properties: {
    TTL: verificationRecords.DKIM2.ttl
    CNAMERecord: {
      cname: '${verificationRecords.DKIM2.value}'
    }
  }
}
