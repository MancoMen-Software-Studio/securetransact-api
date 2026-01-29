using System;
using System.Text.Json;
using FluentAssertions;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.EventStore.JsonConverters;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore.JsonConverters;

public sealed class TransactionIdConverterTests
{
    private readonly JsonSerializerOptions _options;

    public TransactionIdConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _options.Converters.Add(new TransactionIdConverter());
    }

    [Fact]
    public void Write_ShouldSerialize_AsObjectWithValue()
    {
        // Arrange
        TransactionId id = TransactionId.New();

        // Act
        string json = JsonSerializer.Serialize(id, _options);

        // Assert
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        root.GetProperty("value").GetGuid().Should().Be(id.Value);
    }

    [Fact]
    public void Read_ShouldDeserialize_FromObjectWithValue()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        string json = $$$"""{"value":"{{{guid}}}"}""";

        // Act
        TransactionId id = JsonSerializer.Deserialize<TransactionId>(json, _options)!;

        // Assert
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Read_ShouldDeserialize_FromRawGuid()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        string json = $"\"{guid}\"";

        // Act
        TransactionId id = JsonSerializer.Deserialize<TransactionId>(json, _options)!;

        // Assert
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        TransactionId original = TransactionId.New();

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        TransactionId deserialized = JsonSerializer.Deserialize<TransactionId>(json, _options)!;

        // Assert
        deserialized.Value.Should().Be(original.Value);
    }
}
