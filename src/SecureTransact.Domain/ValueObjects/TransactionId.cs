using System;

namespace SecureTransact.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for transactions.
/// Prevents accidental mixing of different ID types.
/// </summary>
public readonly record struct TransactionId
{
    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    private TransactionId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new unique TransactionId.
    /// </summary>
    public static TransactionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a TransactionId from an existing GUID.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>A TransactionId wrapping the provided GUID.</returns>
    public static TransactionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("TransactionId cannot be empty.", nameof(value));
        }

        return new TransactionId(value);
    }

    /// <summary>
    /// Attempts to create a TransactionId from a string.
    /// </summary>
    /// <param name="value">The string representation of a GUID.</param>
    /// <param name="transactionId">The resulting TransactionId if successful.</param>
    /// <returns>True if the string was a valid GUID, false otherwise.</returns>
    public static bool TryParse(string? value, out TransactionId transactionId)
    {
        if (Guid.TryParse(value, out Guid guid) && guid != Guid.Empty)
        {
            transactionId = new TransactionId(guid);
            return true;
        }

        transactionId = default;
        return false;
    }

    /// <summary>
    /// Returns the string representation of the TransactionId.
    /// </summary>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Implicitly converts a TransactionId to a GUID.
    /// </summary>
    public static implicit operator Guid(TransactionId id) => id.Value;
}
