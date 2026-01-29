using System.Collections.Generic;

namespace SecureTransact.Domain.Abstractions;

/// <summary>
/// Marker interface for aggregate roots.
/// Aggregate roots are the only entities that can be directly loaded from repositories.
/// They serve as the entry point for all changes to the aggregate.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the domain events that have been raised by this aggregate.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from the aggregate.
    /// Called after events have been dispatched.
    /// </summary>
    void ClearDomainEvents();
}
