-- SecureTransact Database Initialization Script
-- Creates schemas and tables for Event Store and Read Model

-- Create schemas
CREATE SCHEMA IF NOT EXISTS event_store;
CREATE SCHEMA IF NOT EXISTS read_model;

-- Event Store: Events table with hash chain
CREATE TABLE IF NOT EXISTS event_store.events (
    id UUID PRIMARY KEY,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(500) NOT NULL,
    event_data BYTEA NOT NULL,
    version INT NOT NULL,
    occurred_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    chain_hash BYTEA NOT NULL,
    previous_hash BYTEA,
    global_sequence BIGSERIAL,
    created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for Event Store
CREATE UNIQUE INDEX IF NOT EXISTS ix_events_aggregate_version
    ON event_store.events (aggregate_id, version);
CREATE INDEX IF NOT EXISTS ix_events_aggregate_id
    ON event_store.events (aggregate_id);
CREATE INDEX IF NOT EXISTS ix_events_global_sequence
    ON event_store.events (global_sequence);
CREATE INDEX IF NOT EXISTS ix_events_occurred_at
    ON event_store.events (occurred_at_utc);

-- Read Model: Transactions table (denormalized for queries)
CREATE TABLE IF NOT EXISTS read_model.transactions (
    id UUID PRIMARY KEY,
    source_account_id UUID NOT NULL,
    destination_account_id UUID NOT NULL,
    amount DECIMAL(18,8) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    status VARCHAR(50) NOT NULL,
    reference VARCHAR(256),
    authorization_code VARCHAR(100),
    failure_code VARCHAR(100),
    failure_reason VARCHAR(1000),
    reversal_reason VARCHAR(1000),
    dispute_reason VARCHAR(1000),
    initiated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    authorized_at_utc TIMESTAMP WITH TIME ZONE,
    completed_at_utc TIMESTAMP WITH TIME ZONE,
    failed_at_utc TIMESTAMP WITH TIME ZONE,
    reversed_at_utc TIMESTAMP WITH TIME ZONE,
    disputed_at_utc TIMESTAMP WITH TIME ZONE,
    version INT NOT NULL,
    last_updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Indexes for Read Model
CREATE INDEX IF NOT EXISTS ix_transactions_source_account
    ON read_model.transactions (source_account_id);
CREATE INDEX IF NOT EXISTS ix_transactions_destination_account
    ON read_model.transactions (destination_account_id);
CREATE INDEX IF NOT EXISTS ix_transactions_status
    ON read_model.transactions (status);
CREATE INDEX IF NOT EXISTS ix_transactions_source_initiated
    ON read_model.transactions (source_account_id, initiated_at_utc);
CREATE INDEX IF NOT EXISTS ix_transactions_destination_initiated
    ON read_model.transactions (destination_account_id, initiated_at_utc);

-- Demo: Create view for easy event chain visualization
CREATE OR REPLACE VIEW event_store.event_chain_view AS
SELECT
    e.id as event_id,
    e.aggregate_id,
    e.event_type,
    e.version,
    e.occurred_at_utc,
    encode(e.chain_hash, 'hex') as chain_hash_hex,
    encode(e.previous_hash, 'hex') as previous_hash_hex,
    e.global_sequence,
    CASE
        WHEN e.previous_hash IS NULL THEN 'GENESIS'
        WHEN lag(e.chain_hash) OVER (PARTITION BY e.aggregate_id ORDER BY e.version) = e.previous_hash THEN 'VALID'
        ELSE 'BROKEN'
    END as chain_status
FROM event_store.events e
ORDER BY e.aggregate_id, e.version;

-- Demo: Create view for transaction statistics
CREATE OR REPLACE VIEW read_model.transaction_stats AS
SELECT
    COUNT(*) as total_transactions,
    COUNT(CASE WHEN status = 'Completed' THEN 1 END) as completed,
    COUNT(CASE WHEN status = 'Failed' THEN 1 END) as failed,
    COUNT(CASE WHEN status = 'Reversed' THEN 1 END) as reversed,
    COUNT(CASE WHEN status = 'Disputed' THEN 1 END) as disputed,
    COUNT(CASE WHEN status = 'Initiated' THEN 1 END) as pending,
    COALESCE(SUM(CASE WHEN status = 'Completed' THEN amount END), 0) as total_completed_amount,
    COUNT(DISTINCT source_account_id) as unique_source_accounts,
    COUNT(DISTINCT destination_account_id) as unique_destination_accounts
FROM read_model.transactions;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA event_store TO securetransact;
GRANT ALL PRIVILEGES ON SCHEMA read_model TO securetransact;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA event_store TO securetransact;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA read_model TO securetransact;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA event_store TO securetransact;

COMMENT ON SCHEMA event_store IS 'Event sourcing storage with cryptographic hash chain';
COMMENT ON SCHEMA read_model IS 'CQRS read model for optimized queries';
COMMENT ON TABLE event_store.events IS 'Immutable event log with hash chain for tamper detection';
COMMENT ON COLUMN event_store.events.chain_hash IS 'HMAC-SHA512 hash linking to previous event';
COMMENT ON VIEW event_store.event_chain_view IS 'Visualization of event chain integrity';
