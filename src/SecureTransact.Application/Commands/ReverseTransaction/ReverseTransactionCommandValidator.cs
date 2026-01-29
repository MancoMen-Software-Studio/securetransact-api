using FluentValidation;

namespace SecureTransact.Application.Commands.ReverseTransaction;

/// <summary>
/// Validator for ReverseTransactionCommand.
/// </summary>
public sealed class ReverseTransactionCommandValidator : AbstractValidator<ReverseTransactionCommand>
{
    public ReverseTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason for reversal is required.")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters.");
    }
}
