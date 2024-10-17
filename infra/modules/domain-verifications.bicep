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

// resource Microsoft_Network_dnszones_SOA_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/SOA@2023-07-01-preview' = {
//   parent: dnszones_cecinestpasunmariage_org_name_resource
//   name: '@'
//   properties: {
//     TTL: 3600
//     SOARecord: {
//       email: 'azuredns-hostmaster.microsoft.com'
//       expireTime: 2419200
//       host: 'ns1-32.azure-dns.com.'
//       minimumTTL: 300
//       refreshTime: 3600
//       retryTime: 300
//       serialNumber: 1
//     }
//     targetResource: {}
//     trafficManagementProfile: {}
//   }
// }

// resource Microsoft_Network_dnszones_TXT_dnszones_cecinestpasunmariage_org_name 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
//   parent: dnszones_cecinestpasunmariage_org_name_resource
//   name: '@'
//   properties: {
//     TTL: 3600
//     TXTRecords: [
//       {
//         value: [
//           '17sjpqsqxqf08knbq50dqs76qr1spclv'
//         ]
//       }
//     ]
//     targetResource: {}
//     trafficManagementProfile: {}
//   }
// }

// resource Microsoft_Network_dnszones_TXT_dnszones_cecinestpasunmariage_org_name_dev 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
//   parent: dnszones_cecinestpasunmariage_org_name_resource
//   name: 'dev'
//   properties: {
//     TTL: 3600
//     TXTRecords: [
//       {
//         value: [
//           'ms-domain-verification=0c142b2e-2a05-42e1-a2f2-2f6524ecf26d'
//         ]
//       }
//     ]
//     targetResource: {}
//     trafficManagementProfile: {}
//   }
// }
