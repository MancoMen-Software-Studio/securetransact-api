namespace SecureTransact.Api.Configuration;

/// <summary>
/// Configuration options for Redis cache.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the cache instance name prefix.
    /// </summary>
    public string InstanceName { get; set; } = "SecureTransact:";

    /// <summary>
    /// Gets or sets the default cache expiration in minutes.
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the sliding expiration in minutes.
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 2;
}
