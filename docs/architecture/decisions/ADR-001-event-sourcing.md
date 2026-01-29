# ADR-001: Use Event Sourcing for Transaction History

## Status

Accepted

## Date

2026-01-27

## Context

SecureTransact requires a complete, immutable audit trail of all transaction state changes. Regulated industries (fintech, healthcare, gaming) mandate that organizations can reconstruct the exact state of any transaction at any point in time, prove that records have not been tampered with, and provide detailed audit logs for compliance reviews.

Traditional CRUD-based persistence models only store current state. They cannot answer questions like "what was the transaction status at 3:47 PM on Tuesday?" without additional logging infrastructure. Even with audit tables, proving data integrity and detecting tampering is complex.

## Decision

We will use Event Sourcing as the primary persistence mechanism for transaction aggregates.

### Key aspects:

1. **Append-only event store**: Events are never modified or deleted
2. **Hash chaining**: Each event includes SHA-256 hash of the previous event, creating a tamper-evident chain
3. **State reconstruction**: Aggregate state is rebuilt by replaying events from the stream
4. **Snapshots**: Periodic snapshots optimize read performance for long event streams
5. **Projections**: Read models are built from events via background projectors

### Event store schema:

```sql
CREATE TABLE events (
    id UUID PRIMARY KEY,
    stream_id UUID NOT NULL,
    stream_type VARCHAR(256) NOT NULL,
    event_type VARCHAR(256) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB NOT NULL,
    version BIGINT NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    previous_hash CHAR(64),
    current_hash CHAR(64) NOT NULL,
    CONSTRAINT unique_stream_version UNIQUE (stream_id, version)
) PARTITION BY RANGE (timestamp);
```

## Consequences

### Positive

- **Complete audit trail**: Every state change is recorded permanently
- **Tamper detection**: Hash chain validates data integrity on read
- **Temporal queries**: Can reconstruct state at any point in time
- **Debugging**: Event history provides complete context for issues
- **Compliance**: Meets regulatory requirements for audit trails
- **Event replay**: Can rebuild read models or fix projection bugs

### Negative

- **Increased complexity**: Team must understand event sourcing patterns
- **Storage growth**: Events accumulate over time (mitigated by partitioning)
- **Eventual consistency**: Read models may lag behind write model
- **Schema evolution**: Changing event schemas requires careful versioning
- **Query patterns**: Cannot query current state directly (need projections)

### Mitigations

- Monthly table partitions for efficient archival and querying
- Snapshot mechanism for aggregates with many events
- Strong event schema versioning practices
- Comprehensive documentation and team training

## References

- [Event Sourcing pattern - Microsoft](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [Versioning in an Event Sourced System - Greg Young](https://leanpub.com/esversioning)
