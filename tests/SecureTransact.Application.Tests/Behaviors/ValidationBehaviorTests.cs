using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using SecureTransact.Application.Behaviors;
using SecureTransact.Application.Commands.ProcessTransaction;
using SecureTransact.Application.DTOs;
using SecureTransact.Domain.Abstractions;
using Xunit;

namespace SecureTransact.Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidators()
    {
        // Arrange
        ValidationBehavior<ProcessTransactionCommand, Result<TransactionResponse>> behavior =
            new(Array.Empty<IValidator<ProcessTransactionCommand>>());

        ProcessTransactionCommand command = CreateValidCommand();
        bool nextCalled = false;

        // Act
        await behavior.Handle(
            command,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(CreateResponse()));
            },
            CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        // Arrange
        IValidator<ProcessTransactionCommand> validator = Substitute.For<IValidator<ProcessTransactionCommand>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<ProcessTransactionCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        ValidationBehavior<ProcessTransactionCommand, Result<TransactionResponse>> behavior =
            new([validator]);

        ProcessTransactionCommand command = CreateValidCommand();
        bool nextCalled = false;

        // Act
        await behavior.Handle(
            command,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(CreateResponse()));
            },
            CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        IValidator<ProcessTransactionCommand> validator = Substitute.For<IValidator<ProcessTransactionCommand>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<ProcessTransactionCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new List<ValidationFailure>
            {
                new("Amount", "Amount must be positive")
            }));

        ValidationBehavior<ProcessTransactionCommand, Result<TransactionResponse>> behavior =
            new([validator]);

        ProcessTransactionCommand command = CreateValidCommand();

        // Act
        Result<TransactionResponse> result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success(CreateResponse())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Failed");
        result.Error.Description.Should().Contain("Amount must be positive");
    }

    [Fact]
    public async Task Handle_ShouldCombineMultipleErrors()
    {
        // Arrange
        IValidator<ProcessTransactionCommand> validator = Substitute.For<IValidator<ProcessTransactionCommand>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<ProcessTransactionCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new List<ValidationFailure>
            {
                new("Amount", "Error 1"),
                new("Currency", "Error 2")
            }));

        ValidationBehavior<ProcessTransactionCommand, Result<TransactionResponse>> behavior =
            new([validator]);

        ProcessTransactionCommand command = CreateValidCommand();

        // Act
        Result<TransactionResponse> result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success(CreateResponse())),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Error 1");
        result.Error.Description.Should().Contain("Error 2");
    }

    private static ProcessTransactionCommand CreateValidCommand()
    {
        return new ProcessTransactionCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD"
        };
    }

    private static TransactionResponse CreateResponse()
    {
        return new TransactionResponse
        {
            TransactionId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Status = "Completed",
            InitiatedAtUtc = DateTime.UtcNow
        };
    }
}
