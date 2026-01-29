namespace SecureTransact.Api.Configuration;

/// <summary>
/// Configuration options for security response headers.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SecurityHeaders";

    /// <summary>
    /// Gets or sets whether to enable HSTS header.
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// Gets or sets the Content-Security-Policy header value.
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'";

    /// <summary>
    /// Gets or sets whether to enable X-Content-Type-Options: nosniff.
    /// </summary>
    public bool EnableNoSniff { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-Frame-Options header value.
    /// </summary>
    public string FrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Gets or sets the Referrer-Policy header value.
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Gets or sets whether to remove the Server header.
    /// </summary>
    public bool RemoveServerHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets the Permissions-Policy header value.
    /// </summary>
    public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=()";
}
