namespace SecureTransact.Infrastructure.EventStore;

/// <summary>
/// Configuration settings for the event store.
/// </summary>
public sealed class EventStoreSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "EventStore";

    /// <summary>
    /// Gets or sets whether to verify chain integrity on read.
    /// </summary>
    public bool VerifyChainOnRead { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for reading events.
    /// </summary>
    public int ReadBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use snapshots for aggregate reconstruction.
    /// </summary>
    public bool UseSnapshots { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of events before creating a snapshot.
    /// </summary>
    public int SnapshotThreshold { get; set; } = 100;
}
