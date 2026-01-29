using Microsoft.EntityFrameworkCore;
using SecureTransact.Infrastructure.EventStore;

namespace SecureTransact.Infrastructure.Persistence.Contexts;

/// <summary>
/// Database context for the event store.
/// </summary>
public sealed class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the stored events.
    /// </summary>
    public DbSet<StoredEvent> Events => Set<StoredEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.ToTable("events", "event_store");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.AggregateId)
                .HasColumnName("aggregate_id")
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.EventData)
                .HasColumnName("event_data")
                .IsRequired();

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .IsRequired();

            entity.Property(e => e.OccurredAtUtc)
                .HasColumnName("occurred_at_utc")
                .IsRequired();

            entity.Property(e => e.ChainHash)
                .HasColumnName("chain_hash")
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(e => e.PreviousHash)
                .HasColumnName("previous_hash")
                .HasMaxLength(64);

            entity.Property(e => e.GlobalSequence)
                .HasColumnName("global_sequence")
                .ValueGeneratedOnAdd();

            entity.HasIndex(e => new { e.AggregateId, e.Version })
                .IsUnique()
                .HasDatabaseName("ix_events_aggregate_version");

            entity.HasIndex(e => e.AggregateId)
                .HasDatabaseName("ix_events_aggregate_id");

            entity.HasIndex(e => e.GlobalSequence)
                .HasDatabaseName("ix_events_global_sequence");
        });
    }
}
