using System;
using System.Text;
using System.Text.Json;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Infrastructure.EventStore.JsonConverters;

namespace SecureTransact.Infrastructure.EventStore;

/// <summary>
/// Serializes and deserializes domain events for storage.
/// </summary>
public sealed class EventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions;

    static EventSerializer()
    {
        SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        SerializerOptions.Converters.Add(new MoneyConverter());
        SerializerOptions.Converters.Add(new TransactionIdConverter());
        SerializerOptions.Converters.Add(new AccountIdConverter());
    }

    /// <summary>
    /// Serializes a domain event to bytes.
    /// </summary>
    public byte[] Serialize(IDomainEvent domainEvent)
    {
        string json = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes bytes to a domain event.
    /// </summary>
    public IDomainEvent Deserialize(byte[] data, string eventTypeName)
    {
        Type? eventType = Type.GetType(eventTypeName);
        if (eventType == null)
        {
            throw new InvalidOperationException($"Cannot find event type: {eventTypeName}");
        }

        string json = Encoding.UTF8.GetString(data);
        object? result = JsonSerializer.Deserialize(json, eventType, SerializerOptions);

        if (result is not IDomainEvent domainEvent)
        {
            throw new InvalidOperationException($"Deserialized object is not a domain event: {eventTypeName}");
        }

        return domainEvent;
    }

    /// <summary>
    /// Gets the fully qualified type name for an event.
    /// </summary>
    public string GetEventTypeName(IDomainEvent domainEvent)
    {
        Type type = domainEvent.GetType();
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}

/// <summary>
/// Interface for event serialization.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes a domain event to bytes.
    /// </summary>
    byte[] Serialize(IDomainEvent domainEvent);

    /// <summary>
    /// Deserializes bytes to a domain event.
    /// </summary>
    IDomainEvent Deserialize(byte[] data, string eventTypeName);

    /// <summary>
    /// Gets the fully qualified type name for an event.
    /// </summary>
    string GetEventTypeName(IDomainEvent domainEvent);
}
