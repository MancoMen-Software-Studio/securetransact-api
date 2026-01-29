@description('Base name for all resources')
param baseName string = 'securetransact'
@description('Location for all resources')
param location string = resourceGroup().location
@description('Environment (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'
@description('Container image to deploy')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
@description('PostgreSQL connection string')
@secure()
param postgresConnectionString string
@description('JWT Secret Key')
@secure()
param jwtSecretKey string
@description('Encryption Key (Base64)')
@secure()
param encryptionKey string
@description('HMAC Key (Base64)')
@secure()
param hmacKey string
@description('ACR login server')
param acrLoginServer string = 'securetransactacr.azurecr.io'
@description('ACR username')
param acrUsername string = 'securetransactacr'
@description('ACR password')
@secure()
param acrPassword string
var resourcePrefix = '${baseName}-${environment}'
var tags = {
  Environment: environment
  Application: 'SecureTransact'
  ManagedBy: 'Bicep'
}
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourcePrefix}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${resourcePrefix}-env'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${resourcePrefix}-api'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        corsPolicy: {
          allowedOrigins: ['*']
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
          maxAge: 3600
        }
      }
      registries: [
        {
          server: acrLoginServer
          username: acrUsername
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acrPassword
        }
        {
          name: 'postgres-connection-string'
          value: postgresConnectionString
        }
        {
          name: 'jwt-secret-key'
          value: jwtSecretKey
        }
        {
          name: 'encryption-key'
          value: encryptionKey
        }
        {
          name: 'hmac-key'
          value: hmacKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'securetransact-api'
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'postgres-connection-string'
            }
            {
              name: 'Jwt__SecretKey'
              secretRef: 'jwt-secret-key'
            }
            {
              name: 'Jwt__Issuer'
              value: 'SecureTransact.${environment}'
            }
            {
              name: 'Jwt__Audience'
              value: 'SecureTransact.Api.${environment}'
            }
            {
              name: 'Jwt__ExpirationMinutes'
              value: '60'
            }
            {
              name: 'Cryptography__UseKeyVault'
              value: 'false'
            }
            {
              name: 'Cryptography__EncryptionKey'
              secretRef: 'encryption-key'
            }
            {
              name: 'Cryptography__HmacKey'
              secretRef: 'hmac-key'
            }
            {
              name: 'EventStore__VerifyChainOnRead'
              value: 'true'
            }
            {
              name: 'EventStore__ReadBatchSize'
              value: '100'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerAppName string = containerApp.name
output environmentName string = containerAppEnvironment.name
output logAnalyticsId string = logAnalytics.id
