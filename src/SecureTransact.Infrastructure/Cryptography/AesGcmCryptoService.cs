using System;
using System.Security.Cryptography;

namespace SecureTransact.Infrastructure.Cryptography;

/// <summary>
/// Cryptographic service using AES-256-GCM for encryption and HMAC-SHA512 for authentication.
/// </summary>
public sealed class AesGcmCryptoService : ICryptoService, IDisposable
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _encryptionKey;
    private readonly byte[] _hmacKey;
    private bool _disposed;

    public AesGcmCryptoService(byte[] encryptionKey, byte[] hmacKey)
    {
        if (encryptionKey.Length != KeySize)
        {
            throw new ArgumentException($"Encryption key must be exactly {KeySize} bytes.", nameof(encryptionKey));
        }

        if (hmacKey.Length < 64)
        {
            throw new ArgumentException("HMAC key should be at least 64 bytes for optimal security.", nameof(hmacKey));
        }

        _encryptionKey = new byte[encryptionKey.Length];
        encryptionKey.CopyTo(_encryptionKey, 0);

        _hmacKey = new byte[hmacKey.Length];
        hmacKey.CopyTo(_hmacKey, 0);
    }

    /// <summary>
    /// Encrypts data using AES-256-GCM.
    /// Output format: [nonce (12 bytes)] [tag (16 bytes)] [ciphertext]
    /// </summary>
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        using AesGcm aes = new(_encryptionKey, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSize);
        ciphertext.CopyTo(result, NonceSize + TagSize);

        return result;
    }

    /// <summary>
    /// Decrypts data encrypted with AES-256-GCM.
    /// Expected format: [nonce (12 bytes)] [tag (16 bytes)] [ciphertext]
    /// </summary>
    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ciphertext.Length < NonceSize + TagSize)
        {
            throw new ArgumentException("Ciphertext is too short to contain required nonce and tag.", nameof(ciphertext));
        }

        ReadOnlySpan<byte> nonce = ciphertext[..NonceSize];
        ReadOnlySpan<byte> tag = ciphertext.Slice(NonceSize, TagSize);
        ReadOnlySpan<byte> encryptedData = ciphertext[(NonceSize + TagSize)..];

        byte[] plaintext = new byte[encryptedData.Length];

        using AesGcm aes = new(_encryptionKey, TagSize);
        aes.Decrypt(nonce, encryptedData, tag, plaintext);

        return plaintext;
    }

    /// <summary>
    /// Computes HMAC-SHA512 for the given data.
    /// </summary>
    public byte[] ComputeHmac(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return HMACSHA512.HashData(_hmacKey, data);
    }

    /// <summary>
    /// Verifies HMAC-SHA512 using constant-time comparison.
    /// </summary>
    public bool VerifyHmac(ReadOnlySpan<byte> data, ReadOnlySpan<byte> expectedHmac)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] computedHmac = ComputeHmac(data);
        return CryptographicOperations.FixedTimeEquals(computedHmac, expectedHmac);
    }

    /// <summary>
    /// Computes SHA-512 hash.
    /// </summary>
    public byte[] ComputeHash(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return SHA512.HashData(data);
    }

    /// <summary>
    /// Computes a hash chain value for event integrity.
    /// Chain hash = HMAC(previousHash || eventData)
    /// </summary>
    public byte[] ComputeChainHash(byte[]? previousHash, ReadOnlySpan<byte> eventData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int previousHashLength = previousHash?.Length ?? 0;
        byte[] combined = new byte[previousHashLength + eventData.Length];

        if (previousHash != null)
        {
            previousHash.CopyTo(combined, 0);
        }

        eventData.CopyTo(combined.AsSpan(previousHashLength));

        return ComputeHmac(combined);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_encryptionKey);
        CryptographicOperations.ZeroMemory(_hmacKey);

        _disposed = true;
    }
}
