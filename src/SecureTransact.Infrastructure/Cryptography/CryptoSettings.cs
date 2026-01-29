namespace SecureTransact.Infrastructure.Cryptography;

/// <summary>
/// Configuration settings for cryptographic operations.
/// </summary>
public sealed class CryptoSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Cryptography";

    /// <summary>
    /// Gets or sets the base64-encoded AES-256 encryption key.
    /// Must be exactly 32 bytes when decoded.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base64-encoded HMAC key.
    /// Should be at least 64 bytes when decoded for optimal security.
    /// </summary>
    public string HmacKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use Azure Key Vault for key management.
    /// </summary>
    public bool UseKeyVault { get; set; }

    /// <summary>
    /// Gets or sets the Azure Key Vault URI.
    /// </summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// Gets or sets the name of the encryption key secret in Key Vault.
    /// </summary>
    public string? EncryptionKeySecretName { get; set; }

    /// <summary>
    /// Gets or sets the name of the HMAC key secret in Key Vault.
    /// </summary>
    public string? HmacKeySecretName { get; set; }
}
