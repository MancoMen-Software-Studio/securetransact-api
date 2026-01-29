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

        modelBuilder.Entity<TransactionReadModel>(entity =>
        {
            entity.ToTable("transactions", "read_model");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(t => t.SourceAccountId)
                .HasColumnName("source_account_id")
                .IsRequired();

            entity.Property(t => t.DestinationAccountId)
                .HasColumnName("destination_account_id")
                .IsRequired();

            entity.Property(t => t.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 8)
                .IsRequired();

            entity.Property(t => t.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(t => t.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(t => t.Reference)
                .HasColumnName("reference")
                .HasMaxLength(256);

            entity.Property(t => t.AuthorizationCode)
                .HasColumnName("authorization_code")
                .HasMaxLength(100);

            entity.Property(t => t.FailureCode)
                .HasColumnName("failure_code")
                .HasMaxLength(100);

            entity.Property(t => t.FailureReason)
                .HasColumnName("failure_reason")
                .HasMaxLength(1000);

            entity.Property(t => t.ReversalReason)
                .HasColumnName("reversal_reason")
                .HasMaxLength(1000);

            entity.Property(t => t.DisputeReason)
                .HasColumnName("dispute_reason")
                .HasMaxLength(1000);

            entity.Property(t => t.InitiatedAtUtc)
                .HasColumnName("initiated_at_utc")
                .IsRequired();

            entity.Property(t => t.AuthorizedAtUtc)
                .HasColumnName("authorized_at_utc");

            entity.Property(t => t.CompletedAtUtc)
                .HasColumnName("completed_at_utc");

            entity.Property(t => t.FailedAtUtc)
                .HasColumnName("failed_at_utc");

            entity.Property(t => t.ReversedAtUtc)
                .HasColumnName("reversed_at_utc");

            entity.Property(t => t.DisputedAtUtc)
                .HasColumnName("disputed_at_utc");

            entity.Property(t => t.Version)
                .HasColumnName("version")
                .IsRequired();

            entity.Property(t => t.LastUpdatedAtUtc)
                .HasColumnName("last_updated_at_utc")
                .IsRequired();

            entity.HasIndex(t => t.SourceAccountId)
                .HasDatabaseName("ix_transactions_source_account");

            entity.HasIndex(t => t.DestinationAccountId)
                .HasDatabaseName("ix_transactions_destination_account");

            entity.HasIndex(t => t.Status)
                .HasDatabaseName("ix_transactions_status");

            entity.HasIndex(t => new { t.SourceAccountId, t.InitiatedAtUtc })
                .HasDatabaseName("ix_transactions_source_initiated");

            entity.HasIndex(t => new { t.DestinationAccountId, t.InitiatedAtUtc })
                .HasDatabaseName("ix_transactions_destination_initiated");
        });
    }
}
