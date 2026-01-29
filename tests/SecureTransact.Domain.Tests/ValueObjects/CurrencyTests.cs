using System.Linq;
using FluentAssertions;
using SecureTransact.Domain.ValueObjects;
using Xunit;

namespace SecureTransact.Domain.Tests.ValueObjects;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD", "US Dollar", "$", 2)]
    [InlineData("EUR", "Euro", "€", 2)]
    [InlineData("GBP", "British Pound", "£", 2)]
    [InlineData("JPY", "Japanese Yen", "¥", 0)]
    public void FromCode_ShouldReturnCorrectCurrency(string code, string name, string symbol, int decimalPlaces)
    {
        // Act
        Currency? currency = Currency.FromCode(code);

        // Assert
        currency.Should().NotBeNull();
        currency!.Code.Should().Be(code);
        currency.Name.Should().Be(name);
        currency.Symbol.Should().Be(symbol);
        currency.DecimalPlaces.Should().Be(decimalPlaces);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("USD")]
    public void FromCode_ShouldBeCaseInsensitive(string code)
    {
        // Act
        Currency? currency = Currency.FromCode(code);

        // Assert
        currency.Should().NotBeNull();
        currency!.Code.Should().Be("USD");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("XXX")]
    [InlineData("INVALID")]
    public void FromCode_ShouldReturnNull_WhenCodeIsInvalid(string? code)
    {
        // Act
        Currency? currency = Currency.FromCode(code);

        // Assert
        currency.Should().BeNull();
    }

    [Fact]
    public void IsSupported_ShouldReturnTrue_ForSupportedCurrencies()
    {
        // Assert
        Currency.IsSupported("USD").Should().BeTrue();
        Currency.IsSupported("EUR").Should().BeTrue();
        Currency.IsSupported("GBP").Should().BeTrue();
    }

    [Fact]
    public void IsSupported_ShouldReturnFalse_ForUnsupportedCurrencies()
    {
        // Assert
        Currency.IsSupported("XXX").Should().BeFalse();
        Currency.IsSupported(null).Should().BeFalse();
        Currency.IsSupported("").Should().BeFalse();
    }

    [Fact]
    public void GetAll_ShouldReturnAllSupportedCurrencies()
    {
        // Act
        Currency[] currencies = Currency.GetAll().ToArray();

        // Assert
        currencies.Should().HaveCountGreaterThan(5);
        currencies.Should().Contain(c => c.Code == "USD");
        currencies.Should().Contain(c => c.Code == "EUR");
    }

    [Fact]
    public void StaticProperties_ShouldReturnCorrectCurrencies()
    {
        // Assert
        Currency.USD.Code.Should().Be("USD");
        Currency.EUR.Code.Should().Be("EUR");
        Currency.GBP.Code.Should().Be("GBP");
        Currency.JPY.Code.Should().Be("JPY");
    }

    [Fact]
    public void ToString_ShouldReturnCode()
    {
        // Act
        string result = Currency.USD.ToString();

        // Assert
        result.Should().Be("USD");
    }

    [Fact]
    public void Equality_ShouldBeBasedOnAllProperties()
    {
        // Arrange
        Currency? currency1 = Currency.FromCode("USD");
        Currency? currency2 = Currency.FromCode("USD");

        // Act & Assert
        currency1.Should().Be(currency2);
    }
}
