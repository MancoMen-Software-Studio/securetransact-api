using Microsoft.EntityFrameworkCore;
using SecureTransact.Infrastructure.Persistence.ReadModels;

namespace SecureTransact.Infrastructure.Persistence.Contexts;

/// <summary>
/// Database context for the transaction read model (CQRS query side).
/// </summary>
public sealed class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the transaction read models.
    /// </summary>
    public DbSet<TransactionReadModel> Transactions => Set<TransactionReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new Configurations.TransactionReadModelConfiguration());
    }
}
