using System;
using FluentValidation;
using SecureTransact.Domain.ValueObjects;

namespace SecureTransact.Application.Commands.ProcessTransaction;

/// <summary>
/// Validator for ProcessTransactionCommand.
/// </summary>
public sealed class ProcessTransactionCommandValidator : AbstractValidator<ProcessTransactionCommand>
{
    public ProcessTransactionCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required.");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty()
            .WithMessage("Destination account ID is required.");

        RuleFor(x => x.DestinationAccountId)
            .NotEqual(x => x.SourceAccountId)
            .WithMessage("Source and destination accounts cannot be the same.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code.")
            .Must(BeASupportedCurrency)
            .WithMessage("Currency is not supported.");

        RuleFor(x => x.Reference)
            .MaximumLength(256)
            .WithMessage("Reference cannot exceed 256 characters.");
    }

    private static bool BeASupportedCurrency(string? currency)
    {
        return Currency.IsSupported(currency);
    }
}
