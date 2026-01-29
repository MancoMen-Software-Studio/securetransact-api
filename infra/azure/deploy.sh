#!/bin/bash
# SecureTransact API - Azure Deployment Script
# For Azure Free Tier / Cuenta Gratuita

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}SecureTransact API - Azure Deployment${NC}"
echo -e "${GREEN}========================================${NC}"

# Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-securetransact-rg}"
LOCATION="${LOCATION:-eastus}"
ENVIRONMENT="${ENVIRONMENT:-dev}"
ACR_NAME="${ACR_NAME:-securetransactacr}"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Install it from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}Not logged in to Azure. Logging in...${NC}"
    az login
fi

echo -e "${GREEN}Current subscription:${NC}"
az account show --query "{Name:name, Id:id}" -o table

# Create Resource Group
echo -e "\n${GREEN}Creating Resource Group...${NC}"
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --tags Environment="$ENVIRONMENT" Application=SecureTransact

# Create Azure Container Registry (Free tier: Basic)
echo -e "\n${GREEN}Creating Azure Container Registry...${NC}"
az acr create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$ACR_NAME" \
    --sku Basic \
    --admin-enabled true

# Get ACR credentials
ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)
ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)

echo -e "${GREEN}ACR Login Server: ${ACR_LOGIN_SERVER}${NC}"

# Build and push Docker image
echo -e "\n${GREEN}Building and pushing Docker image...${NC}"
cd "$(dirname "$0")/../.."

az acr build \
    --registry "$ACR_NAME" \
    --image securetransact-api:latest \
    --image securetransact-api:$(git rev-parse --short HEAD) \
    .

# Deploy with Bicep
echo -e "\n${GREEN}Deploying infrastructure with Bicep...${NC}"

# Prompt for secrets if not set
if [ -z "$POSTGRES_CONNECTION_STRING" ]; then
    echo -e "${YELLOW}Enter PostgreSQL connection string (from Neon.tech or Supabase):${NC}"
    read -s POSTGRES_CONNECTION_STRING
fi

if [ -z "$JWT_SECRET" ]; then
    echo -e "${YELLOW}Generating JWT secret key...${NC}"
    JWT_SECRET=$(openssl rand -base64 48)
    echo -e "${GREEN}JWT Secret (save this!): ${JWT_SECRET}${NC}"
fi

if [ -z "$ENCRYPTION_KEY" ]; then
    echo -e "${YELLOW}Generating encryption key (AES-256)...${NC}"
    ENCRYPTION_KEY=$(openssl rand -base64 32)
    echo -e "${GREEN}Encryption Key (save this!): ${ENCRYPTION_KEY}${NC}"
fi

if [ -z "$HMAC_KEY" ]; then
    echo -e "${YELLOW}Generating HMAC key (SHA-512)...${NC}"
    HMAC_KEY=$(openssl rand -base64 64)
    echo -e "${GREEN}HMAC Key (save this!): ${HMAC_KEY}${NC}"
fi

# Deploy Bicep template
az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infra/azure/main.bicep \
    --parameters \
        baseName=securetransact \
        environment="$ENVIRONMENT" \
        containerImage="${ACR_LOGIN_SERVER}/securetransact-api:latest" \
        postgresConnectionString="$POSTGRES_CONNECTION_STRING" \
        jwtSecretKey="$JWT_SECRET" \
        encryptionKey="$ENCRYPTION_KEY" \
        hmacKey="$HMAC_KEY"

# Get deployment outputs
APP_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name main \
    --query "properties.outputs.containerAppUrl.value" -o tsv)

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "API URL: ${GREEN}${APP_URL}${NC}"
echo -e "Health: ${GREEN}${APP_URL}/health${NC}"
echo -e "Swagger: ${GREEN}${APP_URL}/swagger${NC} (if in dev mode)"
echo -e "\n${YELLOW}Next steps:${NC}"
echo "1. Set up your PostgreSQL database (Neon.tech or Supabase)"
echo "2. Run the init-db.sql script on your database"
echo "3. Update the connection string if needed"
echo "4. Test the health endpoint: curl ${APP_URL}/health"
