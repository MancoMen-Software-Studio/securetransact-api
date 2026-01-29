using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Infrastructure.EventStore.JsonConverters;

/// <summary>
/// JSON converter for the AccountId value object.
/// </summary>
public sealed class AccountIdConverter : JsonConverter<AccountId>
{
    public override AccountId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            Guid value = Guid.Empty;
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

                    if (propName.Equals("value", StringComparison.OrdinalIgnoreCase))
                    {
                        value = reader.GetGuid();
                    }
                }
            }
            return AccountId.From(value);
        }

        return AccountId.From(reader.GetGuid());
    }

    public override void Write(Utf8JsonWriter writer, AccountId value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("value", value.Value);
        writer.WriteEndObject();
    }
}
