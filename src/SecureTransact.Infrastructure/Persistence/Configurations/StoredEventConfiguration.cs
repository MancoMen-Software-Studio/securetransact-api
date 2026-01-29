using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureTransact.Infrastructure.EventStore;

namespace SecureTransact.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the StoredEvent entity.
/// Maps to the event_store.events table.
/// </summary>
public sealed class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable("events", "event_store");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.EventData)
            .HasColumnName("event_data")
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(e => e.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(e => e.ChainHash)
            .HasColumnName("chain_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.PreviousHash)
            .HasColumnName("previous_hash")
            .HasMaxLength(64);

        builder.Property(e => e.GlobalSequence)
            .HasColumnName("global_sequence")
            .ValueGeneratedOnAdd();

        builder.HasIndex(e => new { e.AggregateId, e.Version })
            .IsUnique()
            .HasDatabaseName("ix_events_aggregate_version");

        builder.HasIndex(e => e.AggregateId)
            .HasDatabaseName("ix_events_aggregate_id");

        builder.HasIndex(e => e.GlobalSequence)
            .HasDatabaseName("ix_events_global_sequence");
    }
}
