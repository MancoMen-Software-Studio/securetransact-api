# SecureTransact API - Azure Deployment Guide

Guía paso a paso para desplegar SecureTransact API en Azure Container Apps con cuenta gratuita.

## Requisitos Previos

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) instalado
- Cuenta de Azure (gratuita funciona)
- Git instalado

## Paso 1: Configurar Base de Datos PostgreSQL (Gratuito)

Usaremos **Neon.tech** que ofrece PostgreSQL gratuito en la nube.

### 1.1 Crear cuenta en Neon.tech

1. Ve a [neon.tech](https://neon.tech) y crea una cuenta gratuita
2. Crea un nuevo proyecto llamado `securetransact`
3. Selecciona la región más cercana (ej: `us-east-1`)
Aca seleccione - npx neonctl@latest init

### 1.2 Obtener Connection String

1. En el dashboard de Neon, ve a **Connection Details**
2. Copia el connection string que se ve así:
   ```
   postgresql://username:password@ep-xxxx.region.neon.tech/neondb?sslmode=require
   ```

### 1.3 Inicializar la Base de Datos

En el **SQL Editor** de Neon, ejecuta el contenido de `scripts/init-db.sql`:

```sql
-- Copia y pega todo el contenido de scripts/init-db.sql aquí
```

## Paso 2: Configurar Azure

### 2.1 Login a Azure

```bash
az login
```

### 2.2 Verificar suscripción

```bash
az account show
```

### 2.3 Crear Resource Group

```bash
az group create \
  --name securetransact-rg \
  --location eastus
```

## Paso 3: Crear Azure Container Registry

```bash
# Crear ACR (el nombre debe ser único globalmente)
az acr create \
  --resource-group securetransact-rg \
  --name securetransactacr \
  --sku Basic \
  --admin-enabled true
```

## Paso 4: Construir y Subir la Imagen

```bash
# Desde la raíz del proyecto
az acr build \
  --registry securetransactacr \
  --image securetransact-api:v1 \
  .
```

## Paso 5: Desplegar con Bicep

### 5.1 Generar Claves de Seguridad

```bash
# JWT Secret (mínimo 32 caracteres)
openssl rand -base64 48

# Encryption Key (AES-256 = 32 bytes)
openssl rand -base64 32

# HMAC Key (SHA-512 = 64 bytes)
openssl rand -base64 64
```

**¡GUARDA ESTAS CLAVES EN UN LUGAR SEGURO!**

### 5.2 Desplegar Infraestructura

```bash
# Variables (reemplaza con tus valores)
POSTGRES_CONN="postgresql://user:pass@ep-xxx.neon.tech/neondb?sslmode=require"
JWT_SECRET="tu-jwt-secret-generado"
ENCRYPTION_KEY="tu-encryption-key-generada"
HMAC_KEY="tu-hmac-key-generada"

# Desplegar
az deployment group create \
  --resource-group securetransact-rg \
  --template-file infra/azure/main.bicep \
  --parameters \
    baseName=securetransact \
    environment=dev \
    containerImage="securetransactacr.azurecr.io/securetransact-api:v1" \
    postgresConnectionString="$POSTGRES_CONN" \
    jwtSecretKey="$JWT_SECRET" \
    encryptionKey="$ENCRYPTION_KEY" \
    hmacKey="$HMAC_KEY"
```

## Paso 6: Verificar Despliegue

### 6.1 Obtener URL de la Aplicación

```bash
az containerapp show \
  --name securetransact-dev-api \
  --resource-group securetransact-rg \
  --query "properties.configuration.ingress.fqdn" -o tsv
```

### 6.2 Probar Health Check

```bash
curl https://TU-APP-URL/health
```

### 6.3 Probar Demo Endpoints (si está en dev)

```bash
# Simular transacciones
curl -X POST https://TU-APP-URL/api/demo/simulate

# Ver estadísticas
curl https://TU-APP-URL/api/demo/stats

# Ver eventos
curl https://TU-APP-URL/api/demo/events

# Verificar integridad
curl https://TU-APP-URL/api/demo/verify-all
```

## Paso 7: Configurar CI/CD (Opcional)

### 7.1 Crear Service Principal

```bash
az ad sp create-for-rbac \
  --name "securetransact-github" \
  --role contributor \
  --scopes /subscriptions/TU-SUBSCRIPTION-ID/resourceGroups/securetransact-rg \
  --sdk-auth
```

### 7.2 Configurar GitHub Secrets

En tu repositorio de GitHub, ve a **Settings > Secrets and variables > Actions** y añade:

| Secret | Valor |
|--------|-------|
| `AZURE_CREDENTIALS` | Output del comando anterior (JSON completo) |
| `POSTGRES_CONNECTION_STRING` | Tu connection string de Neon |
| `JWT_SECRET` | Tu JWT secret |
| `ENCRYPTION_KEY` | Tu encryption key |
| `HMAC_KEY` | Tu HMAC key |

## Costos Estimados (Tier Gratuito)

| Servicio | Tier | Costo |
|----------|------|-------|
| Azure Container Apps | Consumo | ~$0 (180K vCPU-s gratis/mes) |
| Azure Container Registry | Basic | ~$5/mes |
| Neon PostgreSQL | Free | $0 |
| **Total** | | **~$5/mes** |

## Comandos Útiles

```bash
# Ver logs de la aplicación
az containerapp logs show \
  --name securetransact-dev-api \
  --resource-group securetransact-rg \
  --follow

# Escalar a 0 (para ahorrar)
az containerapp update \
  --name securetransact-dev-api \
  --resource-group securetransact-rg \
  --min-replicas 0 \
  --max-replicas 1

# Reiniciar aplicación
az containerapp revision restart \
  --name securetransact-dev-api \
  --resource-group securetransact-rg \
  --revision REVISION_NAME

# Eliminar todo (cleanup)
az group delete --name securetransact-rg --yes
```

## Troubleshooting

### Error: "Container app failed to start"
- Verifica que el connection string de PostgreSQL sea correcto
- Revisa los logs: `az containerapp logs show ...`

### Error: "Authentication failed"
- Verifica que las claves JWT/Encryption/HMAC estén en Base64 válido
- Regenera las claves si es necesario

### Error: "Database connection failed"
- Verifica que ejecutaste `init-db.sql` en Neon
- Verifica que el connection string tenga `?sslmode=require`

## Próximos Pasos

1. Configurar dominio personalizado
2. Habilitar Application Insights
3. Configurar alertas
4. Implementar Key Vault para secretos
