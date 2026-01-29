# SecureTransact API - Architecture Diagrams

Visual documentation for the SecureTransact API architecture using Mermaid diagrams. This document covers the system's Clean Architecture layers, CQRS pipelines, Event Sourcing mechanics, cryptographic integrity, deployment topology, and more.

> **Note:** All diagrams use [Mermaid](https://mermaid.js.org/) syntax and render natively on GitHub.

---

## Table of Contents

1. [Clean Architecture Layers](#1-clean-architecture-layers)
2. [CQRS Command Pipeline](#2-cqrs-command-pipeline)
3. [CQRS Query Pipeline](#3-cqrs-query-pipeline)
4. [Event Sourcing Write Path](#4-event-sourcing-write-path)
5. [Event Sourcing Read Path](#5-event-sourcing-read-path)
6. [Hash Chain Cryptography](#6-hash-chain-cryptography)
7. [Transaction State Machine](#7-transaction-state-machine)
8. [HTTP Request Pipeline](#8-http-request-pipeline)
9. [Deployment Architecture](#9-deployment-architecture)
10. [Database Schema](#10-database-schema)
11. [Domain Model Class Diagram](#11-domain-model-class-diagram)
12. [Dependency Injection Graph](#12-dependency-injection-graph)

---

## 1. Clean Architecture Layers

The solution follows Clean Architecture with strict inward-pointing dependencies. The Domain layer sits at the core with zero external dependencies, the Application layer references only the Domain, and the outer layers (Infrastructure and API) depend on the inner layers but never the reverse.

```mermaid
graph TD
    subgraph API["API Layer — SecureTransact.Api"]
        E[Endpoints]
        MW[Middleware]
        EXT[Extensions]
        CON[Contracts]
    end

    subgraph APP["Application Layer — SecureTransact.Application"]
        CMD[Commands]
        QRY[Queries]
        HND[Handlers]
        BEH[Behaviors]
        DTO[DTOs]
        VAL[Validators]
    end

    subgraph INFRA["Infrastructure Layer — SecureTransact.Infrastructure"]
        PER[Persistence]
        ES[EventStore]
        CRY[Cryptography]
        QS[QueryServices]
    end

    subgraph DOM["Domain Layer — SecureTransact.Domain"]
        AGG[Aggregates]
        VO[ValueObjects]
        EVT[Events]
        ERR[Errors]
        ABS[Abstractions]
    end

    API -->|references| APP
    API -->|references| INFRA
    APP -->|references| DOM
    INFRA -->|references| DOM
    INFRA -->|references| APP

    style DOM fill:#2d6a4f,color:#fff
    style APP fill:#40916c,color:#fff
    style INFRA fill:#52b788,color:#000
    style API fill:#95d5b2,color:#000
```

---

## 2. CQRS Command Pipeline

Commands (write operations) flow through the MediatR pipeline. Each command passes through validation and logging behaviors before reaching the handler, which interacts with the domain aggregates and persists changes through the repository and unit of work.

```mermaid
sequenceDiagram
    participant Client
    participant Endpoint as API Endpoint
    participant MediatR
    participant VB as ValidationBehavior
    participant LB as LoggingBehavior
    participant Handler as CommandHandler
    participant Aggregate as TransactionAggregate
    participant Repo as IRepository
    participant UoW as IUnitOfWork

    Client->>Endpoint: POST /api/transactions
    Endpoint->>MediatR: Send(ProcessTransactionCommand)
    MediatR->>VB: Handle (validate command)
    VB->>VB: Run FluentValidation rules
    alt Validation fails
        VB-->>MediatR: Result.Failure(ValidationError)
        MediatR-->>Endpoint: Failure response
        Endpoint-->>Client: 400 Bad Request
    end
    VB->>LB: Next()
    LB->>LB: Log command start
    LB->>Handler: Next()
    Handler->>Aggregate: TransactionAggregate.Create(...)
    Aggregate->>Aggregate: RaiseDomainEvent(TransactionInitiated)
    Aggregate-->>Handler: Result.Success(transaction)
    Handler->>Repo: AddAsync(transaction)
    Handler->>UoW: SaveChangesAsync()
    UoW-->>Handler: Commit successful
    Handler-->>LB: Result.Success(TransactionResponse)
    LB->>LB: Log command end + elapsed time
    LB-->>VB: Result
    VB-->>MediatR: Result
    MediatR-->>Endpoint: Result.Success(TransactionResponse)
    Endpoint-->>Client: 201 Created
```

---

## 3. CQRS Query Pipeline

Queries (read operations) follow a separate path that bypasses the domain aggregates entirely. They read directly from the read model database through dedicated query services, ensuring read and write concerns are fully separated.

```mermaid
sequenceDiagram
    participant Client
    participant Endpoint as API Endpoint
    participant MediatR
    participant VB as ValidationBehavior
    participant LB as LoggingBehavior
    participant Handler as QueryHandler
    participant QS as ITransactionQueryService
    participant DB as ReadModel DB

    Client->>Endpoint: GET /api/transactions/{id}
    Endpoint->>MediatR: Send(GetTransactionByIdQuery)
    MediatR->>VB: Handle (validate query)
    VB->>LB: Next()
    LB->>LB: Log query start
    LB->>Handler: Next()
    Handler->>QS: GetByIdAsync(transactionId)
    QS->>DB: SELECT FROM read_model.transactions
    DB-->>QS: Transaction row
    QS-->>Handler: TransactionResponse
    Handler-->>LB: Result.Success(TransactionResponse)
    LB->>LB: Log query end + elapsed time
    LB-->>VB: Result
    VB-->>MediatR: Result
    MediatR-->>Endpoint: Result.Success(TransactionResponse)
    Endpoint-->>Client: 200 OK
```

---

## 4. Event Sourcing Write Path

When a command mutates an aggregate, the resulting domain events are persisted to the event store. Each event is encrypted with AES-256-GCM, hash-chained with HMAC-SHA512 for tamper detection, and assigned a monotonically increasing version number for optimistic concurrency control.

```mermaid
sequenceDiagram
    participant Handler as CommandHandler
    participant Agg as TransactionAggregate
    participant ES as IEventStore
    participant Crypto as ICryptoService
    participant DB as EventStore DB

    Handler->>Agg: Execute domain operation
    Agg->>Agg: Raise domain event(s)
    Handler->>ES: AppendEventsAsync(aggregateId, events, expectedVersion)
    ES->>ES: Retrieve last event hash for stream
    loop For each domain event
        ES->>Crypto: Encrypt event data (AES-256-GCM)
        Crypto-->>ES: Encrypted payload + nonce
        ES->>Crypto: ComputeHash(previousHash + eventData)
        Crypto-->>ES: HMAC-SHA512 hash (currentHash)
        ES->>ES: Build event record (version, hashes, encrypted data)
    end
    ES->>DB: INSERT INTO event_store.events (batch)
    alt Version conflict
        DB-->>ES: Unique constraint violation
        ES-->>Handler: ConcurrencyException
    else Success
        DB-->>ES: Events persisted
        ES-->>Handler: Success
    end
```

---

## 5. Event Sourcing Read Path

Rehydrating an aggregate from the event store involves reading its full event stream, decrypting each event, and validating the hash chain to detect any tampering. The events are then replayed in order to reconstruct the aggregate's current state.

```mermaid
sequenceDiagram
    participant Handler as QueryHandler / CommandHandler
    participant ES as IEventStore
    participant Crypto as ICryptoService
    participant DB as EventStore DB
    participant Agg as TransactionAggregate

    Handler->>ES: LoadStreamAsync(aggregateId)
    ES->>DB: SELECT FROM event_store.events WHERE stream_id = ? ORDER BY version
    DB-->>ES: Event records (ordered)
    loop For each event record
        ES->>Crypto: Decrypt event data (AES-256-GCM)
        Crypto-->>ES: Plaintext event data
        ES->>Crypto: ComputeHash(previousHash + eventData)
        Crypto-->>ES: Computed hash
        ES->>ES: Verify computedHash == storedCurrentHash
        alt Hash mismatch
            ES-->>Handler: TamperDetectedException
        end
    end
    ES->>Agg: Replay events in order
    Agg->>Agg: Apply each event to rebuild state
    Agg-->>ES: Hydrated aggregate
    ES-->>Handler: Aggregate with current state
```

---

## 6. Hash Chain Cryptography

Each event in the store is linked to its predecessor through a cryptographic hash chain. The current event's hash is computed over the previous event's hash concatenated with the current event's data, forming an immutable, tamper-evident sequence. AES-256-GCM provides authenticated encryption of event payloads, while HMAC-SHA512 produces the chain hashes.

```mermaid
flowchart TD
    subgraph Event1["Event 1 (Genesis)"]
        PH1["previous_hash: NULL"]
        ED1["event_data (AES-256-GCM encrypted)"]
        CH1["current_hash: HMAC-SHA512(NULL || event_data)"]
    end

    subgraph Event2["Event 2"]
        PH2["previous_hash: Event1.current_hash"]
        ED2["event_data (AES-256-GCM encrypted)"]
        CH2["current_hash: HMAC-SHA512(prev_hash || event_data)"]
    end

    subgraph Event3["Event 3"]
        PH3["previous_hash: Event2.current_hash"]
        ED3["event_data (AES-256-GCM encrypted)"]
        CH3["current_hash: HMAC-SHA512(prev_hash || event_data)"]
    end

    CH1 -->|"feeds into"| PH2
    CH2 -->|"feeds into"| PH3

    subgraph Crypto["Cryptographic Primitives"]
        AES["AES-256-GCM<br/>Authenticated Encryption<br/>Unique nonce per event"]
        HMAC["HMAC-SHA512<br/>Hash Chain Integrity"]
        RNG["RandomNumberGenerator<br/>Nonce Generation (CSPRNG)"]
    end

    RNG -->|"generates nonce"| AES
    AES -->|"encrypts"| ED1
    AES -->|"encrypts"| ED2
    AES -->|"encrypts"| ED3
    HMAC -->|"computes"| CH1
    HMAC -->|"computes"| CH2
    HMAC -->|"computes"| CH3
```

---

## 7. Transaction State Machine

The transaction lifecycle is modeled as a finite state machine. Each transition corresponds to a domain event and enforces strict business rules about which state changes are permitted. Failed and Reversed are terminal states with no outbound transitions.

```mermaid
stateDiagram-v2
    [*] --> Initiated

    Initiated --> Authorized: TransactionAuthorized
    Initiated --> Failed: TransactionFailed

    Authorized --> Completed: TransactionCompleted
    Authorized --> Failed: TransactionFailed

    Completed --> Reversed: TransactionReversed
    Completed --> Disputed: TransactionDisputed

    Disputed --> Completed: TransactionCompleted
    Disputed --> Reversed: TransactionReversed

    Failed --> [*]
    Reversed --> [*]
```

---

## 8. HTTP Request Pipeline

Incoming HTTP requests traverse a layered middleware pipeline before reaching the endpoint handler. Each middleware component handles a specific cross-cutting concern such as security headers, exception handling, structured logging, authentication, and authorization.

```mermaid
sequenceDiagram
    participant Client
    participant FH as ForwardedHeaders
    participant EH as ExceptionHandlingMiddleware
    participant SL as SerilogRequestLogging
    participant HTTPS as HTTPS Redirection
    participant Auth as Authentication
    participant Authz as Authorization
    participant Router as Endpoint Routing
    participant EP as Endpoint Handler

    Client->>FH: HTTP Request
    FH->>FH: Process X-Forwarded-* headers
    FH->>EH: Next()
    EH->>SL: Next()
    SL->>SL: Start request timer
    SL->>HTTPS: Next()
    HTTPS->>HTTPS: Redirect HTTP to HTTPS
    HTTPS->>Auth: Next()
    Auth->>Auth: Validate JWT / API key
    Auth->>Authz: Next()
    Authz->>Authz: Check policies and claims
    Authz->>Router: Next()
    Router->>EP: Route to matched endpoint
    EP-->>Router: Response
    Router-->>Authz: Response
    Authz-->>Auth: Response
    Auth-->>HTTPS: Response
    HTTPS-->>SL: Response
    SL->>SL: Log request (status, elapsed, path)
    SL-->>EH: Response
    alt Exception thrown
        EH->>EH: Catch, log, map to ProblemDetails
        EH-->>FH: Error response
    else No exception
        EH-->>FH: Response
    end
    FH-->>Client: HTTP Response
```

---

## 9. Deployment Architecture

The production deployment runs on Azure Container Apps with a managed PostgreSQL database hosted on Neon.tech. Container images are built and pushed to Azure Container Registry via GitHub Actions, and secrets are managed through Azure Key Vault.

```mermaid
graph TD
    subgraph GitHub["GitHub"]
        REPO[Source Repository]
        GHA[GitHub Actions CI/CD]
    end

    subgraph Azure["Azure Cloud"]
        ACR[Azure Container Registry]
        subgraph ACA["Azure Container Apps Environment"]
            API_APP[SecureTransact API<br/>Container App]
        end
        KV[Azure Key Vault<br/>Secrets + Keys]
    end

    subgraph Neon["Neon.tech"]
        PG[(PostgreSQL 17<br/>Event Store + Read Model)]
    end

    subgraph Monitoring["Observability"]
        OTEL[OpenTelemetry Collector]
        SEQ[Seq / Application Insights]
    end

    REPO -->|"push / PR"| GHA
    GHA -->|"docker build + push"| ACR
    GHA -->|"deploy"| API_APP
    ACR -->|"pull image"| API_APP
    API_APP -->|"read secrets"| KV
    API_APP -->|"read/write"| PG
    API_APP -->|"export traces + metrics"| OTEL
    OTEL -->|"forward"| SEQ
```

---

## 10. Database Schema

The system uses two separate schemas within PostgreSQL. The `event_store` schema holds the append-only event log with hash-chained integrity. The `read_model` schema holds the denormalized projection optimized for queries.

```mermaid
erDiagram
    event_store_events {
        uuid id PK "gen_random_uuid()"
        uuid stream_id "Aggregate ID"
        varchar stream_type "Aggregate type name"
        varchar event_type "Event class name"
        jsonb event_data "AES-256-GCM encrypted payload"
        jsonb metadata "Correlation, causation, user"
        bigint version "Optimistic concurrency"
        timestamptz timestamp "Event timestamp"
        char_64 previous_hash "SHA hash of previous event"
        char_64 current_hash "SHA hash of this event"
    }

    read_model_transactions {
        uuid id PK "Transaction ID"
        uuid source_account_id "Source account"
        uuid destination_account_id "Destination account"
        decimal amount "Transaction amount"
        varchar currency_code "ISO 4217 currency"
        varchar status "Current transaction status"
        timestamptz created_at "Creation timestamp"
        timestamptz updated_at "Last update timestamp"
        bigint version "Projection version"
    }

    event_store_events ||--o{ read_model_transactions : "projects to"
```

---

## 11. Domain Model Class Diagram

The domain model centers on the TransactionAggregate, which extends AggregateRoot and encapsulates all transaction lifecycle logic. Value objects enforce invariants at creation, and domain events capture every state change for the event store.

```mermaid
classDiagram
    class Entity~TId~ {
        +TId Id
        #Entity(TId id)
    }

    class AggregateRoot~TId~ {
        -List~IDomainEvent~ _domainEvents
        +IReadOnlyCollection~IDomainEvent~ DomainEvents
        +int Version
        #RaiseDomainEvent(IDomainEvent event)
        +ClearDomainEvents()
    }

    class TransactionAggregate {
        +TransactionId Id
        +AccountId SourceAccountId
        +AccountId DestinationAccountId
        +Money Amount
        +TransactionStatus Status
        +DateTime CreatedAt
        +static Create(AccountId, AccountId, Money) Result~TransactionAggregate~
        +Authorize() Result
        +Complete() Result
        +Fail(string reason) Result
        +Reverse(string reason) Result
        +Dispute(string reason) Result
    }

    class TransactionId {
        +Guid Value
        +static New() TransactionId
        +static From(Guid) Result~TransactionId~
    }

    class AccountId {
        +Guid Value
        +static New() AccountId
        +static From(Guid) Result~AccountId~
    }

    class Money {
        +decimal Value
        +Currency Currency
        +static Create(decimal, string) Result~Money~
        +Add(Money) Result~Money~
        +Subtract(Money) Result~Money~
    }

    class Currency {
        +string Code
        +string Name
        +int DecimalPlaces
        +static FromCode(string) Currency
        +static USD Currency$
        +static EUR Currency$
    }

    class TransactionStatus {
        +string Value
        +static Initiated TransactionStatus$
        +static Authorized TransactionStatus$
        +static Completed TransactionStatus$
        +static Failed TransactionStatus$
        +static Reversed TransactionStatus$
        +static Disputed TransactionStatus$
    }

    class IDomainEvent {
        <<interface>>
        +Guid EventId
        +DateTime OccurredOn
    }

    class TransactionInitiatedEvent {
        +TransactionId TransactionId
        +AccountId SourceAccountId
        +AccountId DestinationAccountId
        +Money Amount
    }

    class TransactionAuthorizedEvent {
        +TransactionId TransactionId
        +DateTime AuthorizedAt
    }

    class TransactionCompletedEvent {
        +TransactionId TransactionId
        +DateTime CompletedAt
    }

    class TransactionFailedEvent {
        +TransactionId TransactionId
        +string Reason
    }

    class TransactionReversedEvent {
        +TransactionId TransactionId
        +string Reason
    }

    class TransactionDisputedEvent {
        +TransactionId TransactionId
        +string Reason
    }

    Entity~TId~ <|-- AggregateRoot~TId~
    AggregateRoot~TId~ <|-- TransactionAggregate
    TransactionAggregate *-- TransactionId
    TransactionAggregate *-- AccountId
    TransactionAggregate *-- Money
    TransactionAggregate *-- TransactionStatus
    Money *-- Currency
    IDomainEvent <|.. TransactionInitiatedEvent
    IDomainEvent <|.. TransactionAuthorizedEvent
    IDomainEvent <|.. TransactionCompletedEvent
    IDomainEvent <|.. TransactionFailedEvent
    IDomainEvent <|.. TransactionReversedEvent
    IDomainEvent <|.. TransactionDisputedEvent
    TransactionAggregate ..> IDomainEvent : raises
```

---

## 12. Dependency Injection Graph

Services are registered with appropriate lifetimes to balance performance and correctness. The CryptoService is a singleton since it is stateless and thread-safe. Repositories, the Unit of Work, EventStore, and QueryService are scoped to the HTTP request to ensure proper DbContext lifetime management. MediatR handlers and validators are registered as transient.

```mermaid
graph TD
    subgraph Singleton["Singleton Lifetime"]
        CS[ICryptoService<br/>CryptoService]
    end

    subgraph Scoped["Scoped Lifetime (per HTTP request)"]
        REPO[ITransactionRepository<br/>TransactionRepository]
        UOW[IUnitOfWork<br/>UnitOfWork]
        ESTR[IEventStore<br/>PostgresEventStore]
        QS[ITransactionQueryService<br/>TransactionQueryService]
        ESDB[EventStoreDbContext]
        TRDB[TransactionDbContext]
    end

    subgraph Transient["Transient Lifetime"]
        PTCH[ProcessTransactionCommandHandler]
        RTCH[ReverseTransactionCommandHandler]
        GTBIQ[GetTransactionByIdQueryHandler]
        GTHQ[GetTransactionHistoryQueryHandler]
        VBEH[ValidationBehavior]
        LBEH[LoggingBehavior]
        PVAL[ProcessTransactionCommandValidator]
        RVAL[ReverseTransactionCommandValidator]
    end

    subgraph Registration["Registration Entry Points"]
        INFDI["Infrastructure.DependencyInjection<br/>AddInfrastructure()"]
        APPDI["Application.DependencyInjection<br/>AddApplication()"]
    end

    INFDI -->|registers| CS
    INFDI -->|registers| REPO
    INFDI -->|registers| UOW
    INFDI -->|registers| ESTR
    INFDI -->|registers| QS
    INFDI -->|registers| ESDB
    INFDI -->|registers| TRDB

    APPDI -->|registers| PTCH
    APPDI -->|registers| RTCH
    APPDI -->|registers| GTBIQ
    APPDI -->|registers| GTHQ
    APPDI -->|registers| VBEH
    APPDI -->|registers| LBEH
    APPDI -->|registers| PVAL
    APPDI -->|registers| RVAL

    PTCH -->|depends on| REPO
    PTCH -->|depends on| UOW
    PTCH -->|depends on| ESTR
    RTCH -->|depends on| REPO
    RTCH -->|depends on| UOW
    RTCH -->|depends on| ESTR
    GTBIQ -->|depends on| QS
    GTHQ -->|depends on| QS
    ESTR -->|depends on| ESDB
    ESTR -->|depends on| CS
    REPO -->|depends on| TRDB
    UOW -->|depends on| ESDB
    UOW -->|depends on| TRDB
    QS -->|depends on| TRDB
```

---

*Generated for the SecureTransact API project by MancoMen Software Studio.*
