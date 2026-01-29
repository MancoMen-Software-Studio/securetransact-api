using System;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using SecureTransact.Infrastructure.Cryptography;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Cryptography;

public sealed class AesGcmCryptoServiceTests : IDisposable
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _hmacKey;
    private readonly AesGcmCryptoService _cryptoService;

    public AesGcmCryptoServiceTests()
    {
        _encryptionKey = new byte[32];
        _hmacKey = new byte[64];
        RandomNumberGenerator.Fill(_encryptionKey);
        RandomNumberGenerator.Fill(_hmacKey);
        _cryptoService = new AesGcmCryptoService(_encryptionKey, _hmacKey);
    }

    [Fact]
    public void Encrypt_ShouldProduceValidCiphertext()
    {
        // Arrange
        byte[] plaintext = Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        byte[] ciphertext = _cryptoService.Encrypt(plaintext);

        // Assert
        ciphertext.Should().NotBeNull();
        ciphertext.Length.Should().BeGreaterThan(plaintext.Length);
        // Nonce (12) + Tag (16) + ciphertext length
        ciphertext.Length.Should().Be(12 + 16 + plaintext.Length);
    }

    [Fact]
    public void Decrypt_ShouldRecoverOriginalPlaintext()
    {
        // Arrange
        byte[] originalPlaintext = Encoding.UTF8.GetBytes("Sensitive financial data");
        byte[] ciphertext = _cryptoService.Encrypt(originalPlaintext);

        // Act
        byte[] decryptedPlaintext = _cryptoService.Decrypt(ciphertext);

        // Assert
        decryptedPlaintext.Should().BeEquivalentTo(originalPlaintext);
        Encoding.UTF8.GetString(decryptedPlaintext).Should().Be("Sensitive financial data");
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentCiphertext_ForSamePlaintext()
    {
        // Arrange
        byte[] plaintext = Encoding.UTF8.GetBytes("Same data");

        // Act
        byte[] ciphertext1 = _cryptoService.Encrypt(plaintext);
        byte[] ciphertext2 = _cryptoService.Encrypt(plaintext);

        // Assert - Different nonces should produce different ciphertext
        ciphertext1.Should().NotBeEquivalentTo(ciphertext2);
    }

    [Fact]
    public void Decrypt_ShouldThrow_WhenCiphertextIsTooShort()
    {
        // Arrange
        byte[] shortCiphertext = new byte[10]; // Less than nonce + tag

        // Act
        Action act = () => _cryptoService.Decrypt(shortCiphertext);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Decrypt_ShouldThrow_WhenCiphertextIsTampered()
    {
        // Arrange
        byte[] plaintext = Encoding.UTF8.GetBytes("Original data");
        byte[] ciphertext = _cryptoService.Encrypt(plaintext);

        // Tamper with the ciphertext (modify encrypted data portion)
        ciphertext[ciphertext.Length - 1] ^= 0xFF;

        // Act
        Action act = () => _cryptoService.Decrypt(ciphertext);

        // Assert
        act.Should().Throw<AuthenticationTagMismatchException>();
    }

    [Fact]
    public void ComputeHmac_ShouldProduceConsistentHash()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Transaction data");

        // Act
        byte[] hmac1 = _cryptoService.ComputeHmac(data);
        byte[] hmac2 = _cryptoService.ComputeHmac(data);

        // Assert
        hmac1.Should().BeEquivalentTo(hmac2);
        hmac1.Length.Should().Be(64); // SHA-512 = 64 bytes
    }

    [Fact]
    public void ComputeHmac_ShouldProduceDifferentHash_ForDifferentData()
    {
        // Arrange
        byte[] data1 = Encoding.UTF8.GetBytes("Data 1");
        byte[] data2 = Encoding.UTF8.GetBytes("Data 2");

        // Act
        byte[] hmac1 = _cryptoService.ComputeHmac(data1);
        byte[] hmac2 = _cryptoService.ComputeHmac(data2);

        // Assert
        hmac1.Should().NotBeEquivalentTo(hmac2);
    }

    [Fact]
    public void VerifyHmac_ShouldReturnTrue_ForValidHmac()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Transaction data");
        byte[] hmac = _cryptoService.ComputeHmac(data);

        // Act
        bool isValid = _cryptoService.VerifyHmac(data, hmac);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHmac_ShouldReturnFalse_ForInvalidHmac()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Transaction data");
        byte[] hmac = _cryptoService.ComputeHmac(data);

        // Tamper with HMAC
        hmac[0] ^= 0xFF;

        // Act
        bool isValid = _cryptoService.VerifyHmac(data, hmac);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ComputeHash_ShouldProduceConsistentHash()
    {
        // Arrange
        byte[] data = Encoding.UTF8.GetBytes("Data to hash");

        // Act
        byte[] hash1 = _cryptoService.ComputeHash(data);
        byte[] hash2 = _cryptoService.ComputeHash(data);

        // Assert
        hash1.Should().BeEquivalentTo(hash2);
        hash1.Length.Should().Be(64); // SHA-512 = 64 bytes
    }

    [Fact]
    public void ComputeChainHash_ShouldIncludePreviousHash()
    {
        // Arrange
        byte[] previousHash = new byte[64];
        RandomNumberGenerator.Fill(previousHash);
        byte[] eventData = Encoding.UTF8.GetBytes("Event data");

        // Act
        byte[] chainHash1 = _cryptoService.ComputeChainHash(previousHash, eventData);
        byte[] chainHash2 = _cryptoService.ComputeChainHash(null, eventData);

        // Assert
        chainHash1.Should().NotBeEquivalentTo(chainHash2);
        chainHash1.Length.Should().Be(64);
        chainHash2.Length.Should().Be(64);
    }

    [Fact]
    public void ComputeChainHash_ShouldBeConsistent()
    {
        // Arrange
        byte[] previousHash = new byte[64];
        RandomNumberGenerator.Fill(previousHash);
        byte[] eventData = Encoding.UTF8.GetBytes("Event data");

        // Act
        byte[] chainHash1 = _cryptoService.ComputeChainHash(previousHash, eventData);
        byte[] chainHash2 = _cryptoService.ComputeChainHash(previousHash, eventData);

        // Assert
        chainHash1.Should().BeEquivalentTo(chainHash2);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenEncryptionKeyIsWrongSize()
    {
        // Arrange
        byte[] wrongSizeKey = new byte[16]; // Should be 32

        // Act
        Action act = () => new AesGcmCryptoService(wrongSizeKey, _hmacKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*32 bytes*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenHmacKeyIsTooShort()
    {
        // Arrange
        byte[] shortHmacKey = new byte[32]; // Should be at least 64

        // Act
        Action act = () => new AesGcmCryptoService(_encryptionKey, shortHmacKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*64 bytes*");
    }

    [Fact]
    public void Dispose_ShouldClearSensitiveKeys()
    {
        // Arrange
        byte[] encryptionKey = new byte[32];
        byte[] hmacKey = new byte[64];
        RandomNumberGenerator.Fill(encryptionKey);
        RandomNumberGenerator.Fill(hmacKey);

        AesGcmCryptoService service = new(encryptionKey, hmacKey);

        // Act
        service.Dispose();

        // Assert - Operations should throw after disposal
        byte[] data = Encoding.UTF8.GetBytes("test");
        Action encryptAct = () => service.Encrypt(data);
        Action decryptAct = () => service.Decrypt(new byte[30]);
        Action hmacAct = () => service.ComputeHmac(data);

        encryptAct.Should().Throw<ObjectDisposedException>();
        decryptAct.Should().Throw<ObjectDisposedException>();
        hmacAct.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _cryptoService.Dispose();
    }
}
