using System.Reflection;
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

        modelBuilder.ApplyConfiguration(
            new Configurations.StoredEventConfiguration());
    }
}
