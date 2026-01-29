using System;
using System.Text.Json;
using FluentAssertions;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.EventStore.JsonConverters;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore.JsonConverters;

public sealed class AccountIdConverterTests
{
    private readonly JsonSerializerOptions _options;

    public AccountIdConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _options.Converters.Add(new AccountIdConverter());
    }

    [Fact]
    public void Write_ShouldSerialize_AsObjectWithValue()
    {
        // Arrange
        AccountId id = AccountId.New();

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
        AccountId id = JsonSerializer.Deserialize<AccountId>(json, _options)!;

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
        AccountId id = JsonSerializer.Deserialize<AccountId>(json, _options)!;

        // Assert
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveValue()
    {
        // Arrange
        AccountId original = AccountId.New();

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        AccountId deserialized = JsonSerializer.Deserialize<AccountId>(json, _options)!;

        // Assert
        deserialized.Value.Should().Be(original.Value);
    }
}
