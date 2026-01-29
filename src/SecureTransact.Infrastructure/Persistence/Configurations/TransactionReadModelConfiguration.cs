using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureTransact.Infrastructure.Persistence.ReadModels;

namespace SecureTransact.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the TransactionReadModel entity.
/// Maps to the read_model.transactions table.
/// </summary>
public sealed class TransactionReadModelConfiguration : IEntityTypeConfiguration<TransactionReadModel>
{
    public void Configure(EntityTypeBuilder<TransactionReadModel> builder)
    {
        builder.ToTable("transactions", "read_model");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.SourceAccountId)
            .HasColumnName("source_account_id")
            .IsRequired();

        builder.Property(t => t.DestinationAccountId)
            .HasColumnName("destination_account_id")
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Reference)
            .HasColumnName("reference")
            .HasMaxLength(256);

        builder.Property(t => t.AuthorizationCode)
            .HasColumnName("authorization_code")
            .HasMaxLength(100);

        builder.Property(t => t.FailureCode)
            .HasColumnName("failure_code")
            .HasMaxLength(100);

        builder.Property(t => t.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        builder.Property(t => t.ReversalReason)
            .HasColumnName("reversal_reason")
            .HasMaxLength(1000);

        builder.Property(t => t.DisputeReason)
            .HasColumnName("dispute_reason")
            .HasMaxLength(1000);

        builder.Property(t => t.InitiatedAtUtc)
            .HasColumnName("initiated_at_utc")
            .IsRequired();

        builder.Property(t => t.AuthorizedAtUtc)
            .HasColumnName("authorized_at_utc");

        builder.Property(t => t.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.Property(t => t.FailedAtUtc)
            .HasColumnName("failed_at_utc");

        builder.Property(t => t.ReversedAtUtc)
            .HasColumnName("reversed_at_utc");

        builder.Property(t => t.DisputedAtUtc)
            .HasColumnName("disputed_at_utc");

        builder.Property(t => t.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(t => t.LastUpdatedAtUtc)
            .HasColumnName("last_updated_at_utc")
            .IsRequired();

        builder.HasIndex(t => t.SourceAccountId)
            .HasDatabaseName("ix_transactions_source_account");

        builder.HasIndex(t => t.DestinationAccountId)
            .HasDatabaseName("ix_transactions_destination_account");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("ix_transactions_status");

        builder.HasIndex(t => new { t.SourceAccountId, t.InitiatedAtUtc })
            .HasDatabaseName("ix_transactions_source_initiated");

        builder.HasIndex(t => new { t.DestinationAccountId, t.InitiatedAtUtc })
            .HasDatabaseName("ix_transactions_destination_initiated");
    }
}
