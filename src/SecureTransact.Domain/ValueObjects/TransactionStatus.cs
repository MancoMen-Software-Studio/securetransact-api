using System;
using System.Collections.Generic;

namespace SecureTransact.Domain.ValueObjects;

/// <summary>
/// Represents the status of a transaction in its lifecycle.
/// Implemented as a smart enum for type safety and behavior encapsulation.
/// </summary>
public sealed record TransactionStatus
{
    private static readonly Dictionary<string, TransactionStatus> AllStatuses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Transaction has been created but not yet processed.
    /// </summary>
    public static readonly TransactionStatus Initiated = new("Initiated", "Transaction initiated", canTransitionTo: ["Authorized", "Failed"]);

    /// <summary>
    /// Transaction has been authorized and is pending completion.
    /// </summary>
    public static readonly TransactionStatus Authorized = new("Authorized", "Transaction authorized", canTransitionTo: ["Completed", "Failed"]);

    /// <summary>
    /// Transaction has been successfully completed.
    /// </summary>
    public static readonly TransactionStatus Completed = new("Completed", "Transaction completed", canTransitionTo: ["Reversed", "Disputed"]);

    /// <summary>
    /// Transaction has failed and will not be processed.
    /// </summary>
    public static readonly TransactionStatus Failed = new("Failed", "Transaction failed", canTransitionTo: []);

    /// <summary>
    /// Transaction has been reversed after completion.
    /// </summary>
    public static readonly TransactionStatus Reversed = new("Reversed", "Transaction reversed", canTransitionTo: []);

    /// <summary>
    /// Transaction is under dispute.
    /// </summary>
    public static readonly TransactionStatus Disputed = new("Disputed", "Transaction disputed", canTransitionTo: ["Completed", "Reversed"]);

    /// <summary>
    /// Gets the status name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a human-readable description.
    /// </summary>
    public string Description { get; }

    private readonly HashSet<string> _allowedTransitions;

    private TransactionStatus(string name, string description, string[] canTransitionTo)
    {
        Name = name;
        Description = description;
        _allowedTransitions = new HashSet<string>(canTransitionTo, StringComparer.OrdinalIgnoreCase);
        AllStatuses[name] = this;
    }

    /// <summary>
    /// Checks if a transition to the specified status is allowed.
    /// </summary>
    /// <param name="targetStatus">The target status to transition to.</param>
    /// <returns>True if the transition is valid, false otherwise.</returns>
    public bool CanTransitionTo(TransactionStatus targetStatus)
    {
        if (targetStatus is null)
        {
            return false;
        }

        return _allowedTransitions.Contains(targetStatus.Name);
    }

    /// <summary>
    /// Gets a status by its name.
    /// </summary>
    /// <param name="name">The status name.</param>
    /// <returns>The status if found, null otherwise.</returns>
    public static TransactionStatus? FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return AllStatuses.GetValueOrDefault(name);
    }

    /// <summary>
    /// Gets all defined statuses.
    /// </summary>
    public static IEnumerable<TransactionStatus> GetAll() => AllStatuses.Values;

    /// <summary>
    /// Checks if this is a terminal status (no further transitions allowed).
    /// </summary>
    public bool IsTerminal => _allowedTransitions.Count == 0;

    /// <summary>
    /// Checks if the transaction is in a successful state.
    /// </summary>
    public bool IsSuccessful => Name == Completed.Name || Name == Reversed.Name;

    /// <summary>
    /// Returns the status name.
    /// </summary>
    public override string ToString() => Name;
}
