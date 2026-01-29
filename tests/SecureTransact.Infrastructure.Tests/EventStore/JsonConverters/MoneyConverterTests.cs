using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.EventStore.JsonConverters;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore.JsonConverters;

public sealed class MoneyConverterTests
{
    private readonly JsonSerializerOptions _options;

    public MoneyConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _options.Converters.Add(new MoneyConverter());
    }

    [Fact]
    public void Write_ShouldSerialize_WithAmountAndCurrencyObject()
    {
        // Arrange
        Money money = Money.Create(250.75m, Currency.USD).Value;

        // Act
        string json = JsonSerializer.Serialize(money, _options);

        // Assert
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        root.GetProperty("amount").GetDecimal().Should().Be(250.75m);
        root.GetProperty("currency").GetProperty("code").GetString().Should().Be("USD");
        root.GetProperty("currency").GetProperty("symbol").GetString().Should().Be("$");
        root.GetProperty("currency").GetProperty("decimalPlaces").GetInt32().Should().Be(2);
    }

    [Fact]
    public void Read_ShouldDeserialize_FromFullCurrencyObject()
    {
        // Arrange
        string json = """{"amount":100.50,"currency":{"code":"EUR","symbol":"€","decimalPlaces":2}}""";

        // Act
        Money money = JsonSerializer.Deserialize<Money>(json, _options)!;

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Code.Should().Be("EUR");
    }

    [Fact]
    public void Read_ShouldDeserialize_FromStringCurrency()
    {
        // Arrange
        string json = """{"amount":50.00,"currency":"USD"}""";

        // Act
        Money money = JsonSerializer.Deserialize<Money>(json, _options)!;

        // Assert
        money.Amount.Should().Be(50.00m);
        money.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void RoundTrip_ShouldPreserveValues()
    {
        // Arrange
        Money original = Money.Create(999.99m, Currency.FromCode("GBP")!).Value;

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        Money deserialized = JsonSerializer.Deserialize<Money>(json, _options)!;

        // Assert
        deserialized.Amount.Should().Be(original.Amount);
        deserialized.Currency.Code.Should().Be(original.Currency.Code);
    }

    [Fact]
    public void Read_ShouldThrow_WhenTokenIsNotStartObject()
    {
        // Arrange
        string json = "42";

        // Act
        Action act = () => JsonSerializer.Deserialize<Money>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Read_ShouldThrow_WhenCurrencyCodeIsInvalid()
    {
        // Arrange
        string json = """{"amount":10.00,"currency":"INVALID"}""";

        // Act
        Action act = () => JsonSerializer.Deserialize<Money>(json, _options);

        // Assert
        act.Should().Throw<JsonException>()
            .WithMessage("*Invalid currency code*");
    }

    [Fact]
    public void RoundTrip_ShouldWork_ForMultipleCurrencies()
    {
        // Arrange
        Currency[] currencies = { Currency.USD, Currency.FromCode("EUR")!, Currency.FromCode("GBP")!, Currency.FromCode("JPY")! };

        foreach (Currency currency in currencies)
        {
            Money original = Money.Create(123.45m, currency).Value;

            // Act
            string json = JsonSerializer.Serialize(original, _options);
            Money deserialized = JsonSerializer.Deserialize<Money>(json, _options)!;

            // Assert
            deserialized.Currency.Code.Should().Be(original.Currency.Code);
        }
    }
}
