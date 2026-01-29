using System;

namespace SecureTransact.Infrastructure.Cryptography;

/// <summary>
/// Service for cryptographic operations used in secure transaction processing.
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Encrypts data using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>The encrypted data with nonce and tag prepended.</returns>
    byte[] Encrypt(ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// Decrypts data encrypted with AES-256-GCM.
    /// </summary>
    /// <param name="ciphertext">The encrypted data with nonce and tag.</param>
    /// <returns>The decrypted data.</returns>
    byte[] Decrypt(ReadOnlySpan<byte> ciphertext);

    /// <summary>
    /// Computes HMAC-SHA512 for the given data.
    /// </summary>
    /// <param name="data">The data to compute HMAC for.</param>
    /// <returns>The HMAC-SHA512 hash.</returns>
    byte[] ComputeHmac(ReadOnlySpan<byte> data);

    /// <summary>
    /// Verifies HMAC-SHA512 for the given data.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="expectedHmac">The expected HMAC value.</param>
    /// <returns>True if the HMAC is valid; otherwise, false.</returns>
    bool VerifyHmac(ReadOnlySpan<byte> data, ReadOnlySpan<byte> expectedHmac);

    /// <summary>
    /// Computes SHA-512 hash for the given data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The SHA-512 hash.</returns>
    byte[] ComputeHash(ReadOnlySpan<byte> data);

    /// <summary>
    /// Computes a hash chain value for event integrity.
    /// </summary>
    /// <param name="previousHash">The hash of the previous event (null for first event).</param>
    /// <param name="eventData">The serialized event data.</param>
    /// <returns>The computed chain hash.</returns>
    byte[] ComputeChainHash(byte[]? previousHash, ReadOnlySpan<byte> eventData);
}
