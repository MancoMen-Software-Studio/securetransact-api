namespace SecureTransact.Api.Configuration;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets the maximum number of requests allowed per window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of queued requests.
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}
