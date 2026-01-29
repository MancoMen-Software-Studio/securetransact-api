using System;
using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using SecureTransact.Domain.Events;
using SecureTransact.Domain.ValueObjects;
using SecureTransact.Infrastructure.EventStore;
using Xunit;

namespace SecureTransact.Infrastructure.Tests.EventStore;

public sealed class EventSerializerTests
{
    private readonly EventSerializer _serializer = new();

    [Fact]
    public void Serialize_ShouldProduceValidBytes()
    {
        // Arrange
        TransactionInitiatedEvent @event = CreateTransactionInitiatedEvent();

        // Act
        byte[] data = _serializer.Serialize(@event);

        // Assert
        data.Should().NotBeNull();
        data.Should().NotBeEmpty();
    }

    [Fact]
    public void Deserialize_ShouldRecoverOriginalEvent()
    {
        // Arrange
        TransactionInitiatedEvent originalEvent = CreateTransactionInitiatedEvent();
        byte[] data = _serializer.Serialize(originalEvent);
        string typeName = _serializer.GetEventTypeName(originalEvent);

        // Act
        IDomainEvent deserializedEvent = _serializer.Deserialize(data, typeName);

        // Assert
        deserializedEvent.Should().BeOfType<TransactionInitiatedEvent>();
        TransactionInitiatedEvent result = (TransactionInitiatedEvent)deserializedEvent;
        result.TransactionId.Value.Should().Be(originalEvent.TransactionId.Value);
        result.SourceAccountId.Value.Should().Be(originalEvent.SourceAccountId.Value);
        result.DestinationAccountId.Value.Should().Be(originalEvent.DestinationAccountId.Value);
        result.Amount.Amount.Should().Be(originalEvent.Amount.Amount);
        result.Amount.Currency.Code.Should().Be(originalEvent.Amount.Currency.Code);
    }

    [Fact]
    public void GetEventTypeName_ShouldReturnFullyQualifiedName()
    {
        // Arrange
        TransactionInitiatedEvent @event = CreateTransactionInitiatedEvent();

        // Act
        string typeName = _serializer.GetEventTypeName(@event);

        // Assert
        typeName.Should().Contain("TransactionInitiatedEvent");
        typeName.Should().Contain("SecureTransact.Domain");
    }

    [Fact]
    public void Deserialize_ShouldThrow_WhenTypeNotFound()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("{}");
        string invalidTypeName = "NonExistent.Event, NonExistent.Assembly";

        // Act
        Action act = () => _serializer.Deserialize(data, invalidTypeName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot find event type*");
    }

    [Fact]
    public void RoundTrip_ShouldWork_ForAllEventTypes()
    {
        // Arrange
        IDomainEvent[] events =
        [
            CreateTransactionInitiatedEvent(),
            CreateTransactionAuthorizedEvent(),
            CreateTransactionCompletedEvent(),
            CreateTransactionFailedEvent(),
            CreateTransactionReversedEvent(),
            CreateTransactionDisputedEvent()
        ];

        foreach (IDomainEvent originalEvent in events)
        {
            // Act
            byte[] data = _serializer.Serialize(originalEvent);
            string typeName = _serializer.GetEventTypeName(originalEvent);
            IDomainEvent deserializedEvent = _serializer.Deserialize(data, typeName);

            // Assert
            deserializedEvent.Should().BeOfType(originalEvent.GetType());
            deserializedEvent.EventId.Should().Be(originalEvent.EventId);
        }
    }

    private static TransactionInitiatedEvent CreateTransactionInitiatedEvent()
    {
        return new TransactionInitiatedEvent
        {
            TransactionId = TransactionId.New(),
            SourceAccountId = AccountId.New(),
            DestinationAccountId = AccountId.New(),
            Amount = Money.Create(100.50m, Currency.USD).Value,
            Reference = "Test payment"
        };
    }

    private static TransactionAuthorizedEvent CreateTransactionAuthorizedEvent()
    {
        return new TransactionAuthorizedEvent
        {
            TransactionId = TransactionId.New(),
            AuthorizationCode = "AUTH123456"
        };
    }

    private static TransactionCompletedEvent CreateTransactionCompletedEvent()
    {
        return new TransactionCompletedEvent
        {
            TransactionId = TransactionId.New(),
            CompletedAtUtc = DateTime.UtcNow
        };
    }

    private static TransactionFailedEvent CreateTransactionFailedEvent()
    {
        return new TransactionFailedEvent
        {
            TransactionId = TransactionId.New(),
            FailureCode = "INSUFFICIENT_FUNDS",
            FailureReason = "Insufficient funds in source account"
        };
    }

    private static TransactionReversedEvent CreateTransactionReversedEvent()
    {
        return new TransactionReversedEvent
        {
            TransactionId = TransactionId.New(),
            Reason = "Customer request",
            ReversedAtUtc = DateTime.UtcNow
        };
    }

    private static TransactionDisputedEvent CreateTransactionDisputedEvent()
    {
        return new TransactionDisputedEvent
        {
            TransactionId = TransactionId.New(),
            Reason = "Unauthorized transaction",
            DisputedAtUtc = DateTime.UtcNow
        };
    }
}
