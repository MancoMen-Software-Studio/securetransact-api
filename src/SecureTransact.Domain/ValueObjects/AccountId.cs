using System;

namespace SecureTransact.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for accounts.
/// Prevents accidental mixing of different ID types.
/// </summary>
public readonly record struct AccountId
{
    /// <summary>
    /// Gets the underlying GUID value.
    /// </summary>
    public Guid Value { get; }

    private AccountId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new unique AccountId.
    /// </summary>
    public static AccountId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an AccountId from an existing GUID.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>An AccountId wrapping the provided GUID.</returns>
    public static AccountId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("AccountId cannot be empty.", nameof(value));
        }

        return new AccountId(value);
    }

    /// <summary>
    /// Attempts to create an AccountId from a string.
    /// </summary>
    /// <param name="value">The string representation of a GUID.</param>
    /// <param name="accountId">The resulting AccountId if successful.</param>
    /// <returns>True if the string was a valid GUID, false otherwise.</returns>
    public static bool TryParse(string? value, out AccountId accountId)
    {
        if (Guid.TryParse(value, out Guid guid) && guid != Guid.Empty)
        {
            accountId = new AccountId(guid);
            return true;
        }

        accountId = default;
        return false;
    }

    /// <summary>
    /// Returns the string representation of the AccountId.
    /// </summary>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Implicitly converts an AccountId to a GUID.
    /// </summary>
    public static implicit operator Guid(AccountId id) => id.Value;
}
