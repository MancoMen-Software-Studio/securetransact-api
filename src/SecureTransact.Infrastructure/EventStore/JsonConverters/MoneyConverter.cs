using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Infrastructure.EventStore.JsonConverters;

/// <summary>
/// JSON converter for the Money value object.
/// </summary>
public sealed class MoneyConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        decimal amount = 0;
        string currencyCode = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName.ToLowerInvariant())
            {
                case "amount":
                    amount = reader.GetDecimal();
                    break;
                case "currency":
                    currencyCode = ReadCurrency(ref reader);
                    break;
            }
        }

        Currency? currency = Currency.FromCode(currencyCode);
        if (currency is null)
        {
            throw new JsonException($"Invalid currency code: {currencyCode}");
        }

        return Money.Create(amount, currency).Value;
    }

    private static string ReadCurrency(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string code = string.Empty;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propName = reader.GetString()!;
                    reader.Read();

                    if (propName.Equals("code", StringComparison.OrdinalIgnoreCase))
                    {
                        code = reader.GetString()!;
                    }
                }
            }
            return code;
        }

        return reader.GetString()!;
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WritePropertyName("currency");
        writer.WriteStartObject();
        writer.WriteString("code", value.Currency.Code);
        writer.WriteString("symbol", value.Currency.Symbol);
        writer.WriteNumber("decimalPlaces", value.Currency.DecimalPlaces);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
