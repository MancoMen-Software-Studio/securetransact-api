using FluentAssertions;
using SecureTransact.Infrastructure.Cryptography;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.Cryptography;

public sealed class CryptoSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeCryptography()
    {
        // Assert
        CryptoSettings.SectionName.Should().Be("Cryptography");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        CryptoSettings settings = new();

        // Assert
        settings.EncryptionKey.Should().BeEmpty();
        settings.HmacKey.Should().BeEmpty();
        settings.UseKeyVault.Should().BeFalse();
        settings.KeyVaultUri.Should().BeNull();
        settings.EncryptionKeySecretName.Should().BeNull();
        settings.HmacKeySecretName.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Act
        CryptoSettings settings = new()
        {
            EncryptionKey = "base64key==",
            HmacKey = "base64hmac==",
            UseKeyVault = true,
            KeyVaultUri = "https://myvault.vault.azure.net",
            EncryptionKeySecretName = "enc-key",
            HmacKeySecretName = "hmac-key"
        };

        // Assert
        settings.EncryptionKey.Should().Be("base64key==");
        settings.HmacKey.Should().Be("base64hmac==");
        settings.UseKeyVault.Should().BeTrue();
        settings.KeyVaultUri.Should().Be("https://myvault.vault.azure.net");
        settings.EncryptionKeySecretName.Should().Be("enc-key");
        settings.HmacKeySecretName.Should().Be("hmac-key");
    }
}
