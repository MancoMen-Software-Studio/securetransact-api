# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SecureTransact API** is a reference implementation by MancoMen Software Studio demonstrating enterprise patterns for secure financial transaction processing. It showcases cryptographic integrity verification, immutable audit trails, and regulatory compliance patterns applicable to fintech, healthcare, gaming, and government sectors.

**Status: FULLY IMPLEMENTED** — All 4 architectural layers are complete with 207 passing tests.

### Repository

- **Organization**: MancoMen Software Studio
- **URL**: https://github.com/MancoMen-Software-Studio/securetransact-api
- **License**: MIT

---

## Build and Test Commands

```bash
# Build
dotnet build

# Run all tests (207 tests)
dotnet test

# Run specific test project
dotnet test tests/SecureTransact.Domain.Tests
dotnet test tests/SecureTransact.Application.Tests
dotnet test tests/SecureTransact.Infrastructure.Tests
dotnet test tests/SecureTransact.Api.Tests
dotnet test tests/SecureTransact.Architecture.Tests

# Run single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Format code
dotnet format

# Run the API
dotnet run --project src/SecureTransact.Api

# Start local dependencies (PostgreSQL, Redis, Seq, pgAdmin)
docker-compose up -d

# EF Core migrations
dotnet ef database update --project src/SecureTransact.Infrastructure
```

---

## Critical Notes

### .NET SDK Version

The project targets **net9.0**. A `global.json` file pins the SDK to `9.0.x` with `rollForward: latestFeature`. If you have .NET 10 installed locally, the global.json ensures the correct SDK is used.

If you see `net10.0` residual folders in `bin/` or `obj/`, delete them:

```bash
find . -type d -name "net10.0" -path "*/bin/*" -o -type d -name "net10.0" -path "*/obj/*" | xargs rm -rf
```

### IDE Errors

If Rider or VS Code shows framework resolution errors, ensure `global.json` exists at the repo root with SDK version `9.0.100` and `rollForward: latestFeature`.

---

## Technology Stack

| Category | Technology | Version |
|----------|------------|---------|
| Runtime | .NET | 9.0 |
| Language | C# | Latest (nullable enabled, implicit usings disabled) |
| Framework | ASP.NET Core Minimal APIs | 9.0.1 |
| ORM | Entity Framework Core | 9.0.1 |
| Database | PostgreSQL (Npgsql) | 17 / Npgsql 9.0.3 |
| Cache | Redis (StackExchange.Redis) | 7.4 / 2.8.24 |
| CQRS | MediatR | 12.4.1 |
| Validation | FluentValidation | 11.11.0 |
| Security | Azure Key Vault | 4.7.0 |
| Auth | JWT Bearer | 9.0.1 |
| Observability | OpenTelemetry | 1.11.2 |
| Logging | Serilog + Seq | 4.2.0 / 9.0.0 |
| API Docs | Scalar (OpenAPI) | 2.0.18 |
| Testing | xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0 |
| Test Infra | Testcontainers 4.3.0, Bogus 35.6.1, NetArchTest.Rules 1.3.2 |

---

## Architecture

Clean Architecture with CQRS and Event Sourcing. All dependencies point inward.

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                            │
│  Endpoints, Middleware, Extensions, Contracts               │
│  References: Application, Infrastructure                    │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  Commands, Queries, Handlers, Validators, DTOs, Behaviors   │
│  References: Domain only                                    │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                           │
│  Aggregates, Value Objects, Events, Errors, Abstractions    │
│  References: NOTHING (zero dependencies)                    │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  Persistence, EventStore, Cryptography, QueryServices       │
│  References: Domain, Application                            │
└─────────────────────────────────────────────────────────────┘
```

### Strict Rules

1. **Domain Layer has ZERO external dependencies** — No NuGet packages, only .NET BCL
2. **Application Layer references only Domain** — Never Infrastructure
3. **All dependencies point inward** — Outer layers depend on inner layers, never reverse
4. **Interfaces in Domain/Application, implementations in Infrastructure**
5. **Architecture tests enforce these rules** — NetArchTest.Rules validates layer boundaries

---

## Project Structure

```
securetransact-api/
├── global.json                          # SDK 9.0.x pinning
├── Directory.Build.props                # Shared build properties (net9.0)
├── Directory.Packages.props             # Centralized NuGet versions
├── docker-compose.yml                   # PostgreSQL 17, Redis 7.4, Seq, pgAdmin
│
├── src/
│   ├── SecureTransact.Domain/           # ZERO dependencies
│   │   ├── Abstractions/                # Entity, AggregateRoot, Result, DomainError,
│   │   │                                # IDomainEvent, IRepository, IEventStore, IUnitOfWork
│   │   ├── Aggregates/                  # TransactionAggregate (event-sourced)
│   │   ├── ValueObjects/                # Money, Currency, TransactionId, AccountId, TransactionStatus
│   │   ├── Events/                      # 7 domain events (Initiated, Authorized, Completed, etc.)
│   │   └── Errors/                      # TransactionErrors, AccountErrors
│   │
│   ├── SecureTransact.Application/      # References Domain only
│   │   ├── Abstractions/                # ICommand, IQuery, ITransactionRepository, ITransactionQueryService
│   │   ├── Commands/                    # ProcessTransaction/, ReverseTransaction/
│   │   ├── Queries/                     # GetTransactionById/, GetTransactionHistory/
│   │   ├── Behaviors/                   # ValidationBehavior, LoggingBehavior
│   │   ├── DTOs/                        # TransactionResponse, TransactionHistoryResponse
│   │   └── DependencyInjection.cs       # MediatR + validators + behaviors registration
│   │
│   ├── SecureTransact.Infrastructure/   # References Domain + Application
│   │   ├── Cryptography/                # AesGcmCryptoService (AES-256-GCM + HMAC-SHA512)
│   │   ├── EventStore/                  # PostgresEventStore (hash-chained, encrypted)
│   │   │   └── JsonConverters/          # Custom converters for domain types
│   │   ├── Persistence/                 # EF Core contexts, repositories, read models, UoW
│   │   │   ├── Contexts/                # EventStoreDbContext, TransactionDbContext
│   │   │   ├── Repositories/            # TransactionRepository
│   │   │   └── ReadModels/              # TransactionReadModel
│   │   ├── QueryServices/               # TransactionQueryService
│   │   └── DependencyInjection.cs       # Full infrastructure registration
│   │
│   └── SecureTransact.Api/              # References Application + Infrastructure
│       ├── Endpoints/                   # HealthEndpoints, TransactionEndpoints, DemoEndpoints
│       ├── Middleware/                   # ExceptionHandlingMiddleware
│       ├── Extensions/                  # OpenApi, Authentication, Logging extensions
│       ├── Contracts/                   # ApiContracts (request/response DTOs)
│       └── Program.cs                   # Minimal API entry point
│
├── tests/
│   ├── SecureTransact.Domain.Tests/         # 133 tests — 90% coverage target
│   ├── SecureTransact.Application.Tests/    # 35 tests  — 85% coverage target
│   ├── SecureTransact.Infrastructure.Tests/ # 20 tests  — 70% coverage target
│   ├── SecureTransact.Api.Tests/            # 7 tests   — 60% coverage target
│   └── SecureTransact.Architecture.Tests/   # 12 tests  — Layer boundary validation
│
├── docs/
│   ├── architecture/
│   │   ├── README.md
│   │   ├── ARCHITECTURE-DIAGRAMS.md     # Mermaid diagrams
│   │   └── decisions/ADR-001-event-sourcing.md
│   └── guides/
│       └── BEGINNER-GUIDE.md            # Spanish beginner guide
│
└── scripts/
    ├── init-db.sql                      # PostgreSQL initialization
    └── pgadmin-servers.json             # pgAdmin auto-config
```

---

## Implementation Details by Layer

### Domain Layer

- **Result Pattern**: Monadic `Result<T>` with `Map`, `Bind`, `Tap`, `Match`. `DomainError` with factory methods (`Validation`, `NotFound`, `Conflict`). Used instead of exceptions for expected business failures.
- **Value Objects**: Immutable records validated at creation via factory methods (`Money.Create()`, `Currency.FromCode()`). Money supports currency-aware arithmetic. Currency supports 11 ISO 4217 codes.
- **TransactionAggregate**: Full event sourcing — `Create`, `Authorize`, `Complete`, `Fail`, `Reverse`, `Dispute`. Each method validates state transitions via TransactionStatus smart enum and raises domain events. `LoadFromHistory()` reconstitutes state from events.
- **TransactionStatus**: Smart enum with state machine — `Initiated -> Authorized/Failed`, `Authorized -> Completed/Failed`, `Completed -> Reversed/Disputed`, `Disputed -> Completed/Reversed`. Terminal states: `Failed`, `Reversed`.

### Application Layer

- **CQRS**: Commands (`ProcessTransactionCommand`, `ReverseTransactionCommand`) and Queries (`GetTransactionByIdQuery`, `GetTransactionHistoryQuery`) via MediatR.
- **Pipeline Behaviors**: `ValidationBehavior` integrates FluentValidation. `LoggingBehavior` logs request/response with timing.
- **Handlers**: `ProcessTransactionCommandHandler` validates money, creates aggregate, simulates authorization, persists via repository + UoW. Returns `TransactionResponse` DTO.

### Infrastructure Layer

- **Cryptography** (`AesGcmCryptoService`): AES-256-GCM encryption with 12-byte nonce + 16-byte auth tag. HMAC-SHA512 for hash chaining. Uses `RandomNumberGenerator` (never `System.Random`). Secure key disposal via `CryptographicOperations.ZeroMemory`. Constant-time comparisons via `FixedTimeEquals`.
- **Event Store** (`PostgresEventStore`): Hash-chained events with integrity verification on read. AES-256-GCM encrypted event data. Optimistic concurrency via version field. Configurable chain verification (`VerifyChainOnRead`).
- **Persistence**: Two separate `DbContext` instances — `EventStoreDbContext` (event_store schema) and `TransactionDbContext` (read_model schema). Both use Npgsql with retry policies.
- **DI Registration**: Azure Key Vault integration with local fallback. Scoped: repositories, UoW, event store, query service. Singleton: crypto service.

### API Layer

- **Middleware Pipeline**: ForwardedHeaders -> ExceptionHandlingMiddleware -> SerilogRequestLogging -> HTTPS Redirection -> Authentication -> Authorization -> Endpoints.
- **Endpoints**: Health checks, Transaction CRUD, Demo endpoints (dev only).
- **Auth**: JWT Bearer authentication with configurable issuer/audience/expiration.
- **Observability**: OpenTelemetry tracing + Serilog structured logging to Console + Seq.

---

## Cryptographic Security

| Operation | Algorithm | Details |
|-----------|-----------|---------|
| Event Data Encryption | AES-256-GCM | 12-byte nonce, 16-byte auth tag, unique nonce per event |
| Hash Chaining | HMAC-SHA512 | Chain links each event to its predecessor for tamper detection |
| Nonce Generation | CSPRNG | `RandomNumberGenerator` only — never `System.Random` |
| Key Management | Azure Key Vault | Production keys from Key Vault, config fallback for dev |
| Key Disposal | ZeroMemory | `CryptographicOperations.ZeroMemory` on sensitive buffers |
| Comparison | Constant-time | `CryptographicOperations.FixedTimeEquals` for hash validation |

### Security Rules

1. **NEVER use `System.Random`** — Always `RandomNumberGenerator`
2. **NEVER hardcode production secrets** — Azure Key Vault in production
3. **NEVER reuse nonces** — Unique nonce per encryption operation
4. **ALWAYS validate hash chains** — Configurable via `EventStore:VerifyChainOnRead`
5. **ALWAYS use constant-time comparisons** — Prevent timing attacks

---

## Event Sourcing

### Event Store Schema

```sql
CREATE TABLE event_store.events (
    id UUID PRIMARY KEY,
    stream_id UUID NOT NULL,          -- Aggregate ID
    stream_type VARCHAR(256) NOT NULL, -- Aggregate type name
    event_type VARCHAR(256) NOT NULL,  -- Event class name
    event_data JSONB NOT NULL,         -- AES-256-GCM encrypted event payload
    metadata JSONB NOT NULL,           -- Correlation, causation, user context
    version BIGINT NOT NULL,           -- Optimistic concurrency control
    timestamp TIMESTAMPTZ NOT NULL,
    previous_hash CHAR(128),           -- HMAC-SHA512 of previous event (null for first)
    current_hash CHAR(128) NOT NULL,   -- HMAC-SHA512 of this event
    CONSTRAINT unique_stream_version UNIQUE (stream_id, version)
);
```

### Event Types

- `TransactionInitiatedEvent`
- `TransactionAuthorizedEvent`
- `TransactionCompletedEvent`
- `TransactionFailedEvent`
- `TransactionReversedEvent`
- `TransactionDisputedEvent`

---

## Local Development

### Docker Compose Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| PostgreSQL | postgres:17-alpine | 5432 | Database |
| Redis | redis:7.4-alpine | 6379 | Cache |
| Seq | datalust/seq:2024.3 | 5341 / 8081 | Log aggregation |
| pgAdmin | dpage/pgadmin4:8.14 | 5050 | DB visualization |

```bash
docker-compose up -d
dotnet run --project src/SecureTransact.Api
```

### Configuration

Key settings in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection string
- `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience` — JWT auth config
- `Cryptography:UseKeyVault` — `false` for local dev (uses config keys)
- `Cryptography:EncryptionKey`, `Cryptography:HmacKey` — Base64-encoded keys
- `EventStore:VerifyChainOnRead` — Enable hash chain verification on reads

---

## Code Style

### Naming Conventions

- **Classes/Interfaces**: PascalCase (`TransactionAggregate`, `IEventStore`)
- **Methods**: PascalCase (`ProcessTransaction`, `GetByIdAsync`)
- **Properties**: PascalCase (`AccountId`, `Amount`)
- **Private fields**: _camelCase (`_repository`, `_logger`)
- **Parameters/locals**: camelCase (`transactionId`, `amount`)
- **Constants**: PascalCase (`MaxRetryAttempts`)

### Style Rules

- Explicit types preferred over `var`
- File-scoped namespaces
- One class per file
- XML documentation on all public APIs
- Warnings treated as errors (`TreatWarningsAsErrors: true`)
- Implicit usings disabled
- Braces required for all control statements

---

## Commit Convention

Follow Conventional Commits: `<type>(<scope>): <description>`

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`

```
feat(domain): add Money value object with validation
fix(crypto): resolve nonce reuse vulnerability in AES encryption
test(domain): add unit tests for TransactionAggregate
refactor(eventstore): optimize hash chain validation
docs(claude): update CLAUDE.md with accurate project state
```

---

## Test Coverage Requirements

| Layer | Tests | Target Coverage |
|-------|-------|-----------------|
| Domain | 133 | 90% |
| Application | 35 | 85% |
| Infrastructure | 20 | 70% |
| API | 7 | 60% |
| Architecture | 12 | N/A (pass/fail) |
| **Total** | **207** | |

---

## Quality Gates

- Zero critical/high SonarQube issues
- Cyclomatic complexity < 10 per method
- Cognitive complexity < 15
- No empty catch blocks
- No TODO comments in main branch
- All 207 tests must pass
- All architecture tests must pass (layer boundary enforcement)

---

## References

- [Architecture Diagrams](docs/architecture/ARCHITECTURE-DIAGRAMS.md)
- [Beginner Guide (Spanish)](docs/guides/BEGINNER-GUIDE.md)
- [Architecture Overview](docs/architecture/README.md)
- [ADR-001: Event Sourcing](docs/architecture/decisions/ADR-001-event-sourcing.md)
- [MancoMen GitHub](https://github.com/MancoMen-Software-Studio)
