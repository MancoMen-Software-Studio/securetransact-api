<div align="center">

# SecureTransact API

**Enterprise-grade secure transaction processing with Event Sourcing**

[![Build Status](https://github.com/MancoMen-Software-Studio/securetransact-api/actions/workflows/ci.yml/badge.svg)](https://github.com/MancoMen-Software-Studio/securetransact-api/actions)
[![codecov](https://codecov.io/gh/MancoMen-Software-Studio/securetransact-api/branch/main/graph/badge.svg)](https://codecov.io/gh/MancoMen-Software-Studio/securetransact-api)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

[Documentation](docs/) •
[API Reference](docs/api/) •
[Architecture](docs/architecture/)

</div>

---

## Overview

SecureTransact is a reference implementation demonstrating enterprise patterns for secure transaction processing in regulated industries. Built with Clean Architecture, CQRS, and Event Sourcing.

### Key Features

- **Clean Architecture** — Domain-centric design with zero infrastructure dependencies
- **CQRS** — Separate read/write models for optimized performance
- **Event Sourcing** — Complete audit trail with hash-chained events
- **Cryptographic Security** — HMAC-SHA256 signing, CSPRNG tokens, AES-256-GCM
- **Compliance Ready** — Designed for fintech, healthcare, and gaming regulations
- **Observable** — OpenTelemetry integration for distributed tracing

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         API Layer                           │
│   Endpoints  │  Middleware  │  Filters  │  Authentication   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                       │
│      Commands  │  Queries  │  Handlers  │  Validators       │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                       Domain Layer                          │
│    Aggregates  │  Events  │  Value Objects  │  Interfaces   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
│     Event Store  │  Repositories  │  Caching  │  Crypto     │
└─────────────────────────────────────────────────────────────┘
```

## Quick Start

```bash
# Clone the repository
git clone https://github.com/MancoMen-Software-Studio/Secure-Transact-API.git
cd Secure-Transact-API

# Start dependencies (PostgreSQL, Redis)
docker-compose up -d

# Run the API
dotnet run --project src/SecureTransact.Api

# API is now running at https://localhost:5001
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (for PostgreSQL and Redis)
- [PostgreSQL 17](https://www.postgresql.org/) (or use Docker)
- [Redis 7.4](https://redis.io/) (or use Docker)

## Installation

### Using Docker (Recommended)

```bash
# Build and run everything
docker-compose -f docker-compose.full.yml up --build
```

### Manual Setup

1. **Clone and restore**
   ```bash
   git clone https://github.com/MancoMen-Software-Studio/Secure-Transact-API.git
   cd Secure-Transact-API
   dotnet restore
   ```

2. **Configure environment**
   ```bash
   cp src/SecureTransact.Api/appsettings.Development.example.json \
      src/SecureTransact.Api/appsettings.Development.json
   # Edit the file with your settings
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update --project src/SecureTransact.Infrastructure
   ```

4. **Start the API**
   ```bash
   dotnet run --project src/SecureTransact.Api
   ```

## Usage

### Process a Transaction

```http
POST /api/v1/transactions
Content-Type: application/json
Authorization: Bearer <token>

{
  "sourceAccountId": "acc_123",
  "destinationAccountId": "acc_456",
  "amount": {
    "value": 100.00,
    "currency": "USD"
  },
  "reference": "INV-2024-001"
}
```

### Query Transaction History

```http
GET /api/v1/accounts/acc_123/transactions?from=2024-01-01&to=2024-12-31
Authorization: Bearer <token>
```

### Verify Event Chain Integrity

```http
POST /api/v1/audit/verify-chain
Content-Type: application/json

{
  "streamId": "account-acc_123",
  "fromVersion": 1,
  "toVersion": 100
}
```

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__Database` | PostgreSQL connection string | — |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` |
| `Jwt__Secret` | JWT signing key (min 32 chars) | — |
| `Jwt__Issuer` | JWT issuer | `SecureTransact` |
| `Jwt__ExpirationMinutes` | Token expiration | `15` |
| `Cryptography__HmacKey` | HMAC signing key | — |

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/SecureTransact.Domain.Tests
```

### Test Coverage Requirements

| Layer | Minimum Coverage |
|-------|------------------|
| Domain | 90% |
| Application | 85% |
| Infrastructure | 70% |
| API | 60% |

## Project Structure

```
SecureTransact/
├── src/
│   ├── SecureTransact.Domain/          # Business logic, zero dependencies
│   ├── SecureTransact.Application/     # Use cases, CQRS handlers
│   ├── SecureTransact.Infrastructure/  # Database, external services
│   └── SecureTransact.Api/             # HTTP endpoints
├── tests/
│   ├── SecureTransact.Domain.Tests/
│   ├── SecureTransact.Application.Tests/
│   ├── SecureTransact.Infrastructure.Tests/
│   ├── SecureTransact.Api.Tests/
│   └── SecureTransact.Architecture.Tests/
├── docs/
│   ├── architecture/
│   └── api/
└── infra/
    ├── terraform/
    ├── docker/
    └── k8s/
```

## Deployment

See [Deployment Guide](docs/guides/deployment.md) for detailed instructions.

### Azure Container Apps

```bash
az containerapp up \
  --name securetransact-api \
  --resource-group mancomen-rg \
  --environment mancomen-env \
  --source .
```

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file.

## Acknowledgments

- [MediatR](https://github.com/jbogard/MediatR) by Jimmy Bogard
- [FluentValidation](https://github.com/FluentValidation/FluentValidation) by Jeremy Skinner

---

<div align="center">

**Built with ❤️ by [MancoMen Software Studio](https://mancomen.com)**

</div>
