using System;
using System.Collections.Generic;

namespace SecureTransact.Domain.Abstractions;

/// <summary>
/// Base class for all domain entities.
/// Entities have identity and are compared by their ID, not their attributes.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Determines whether two entities are equal based on their IDs.
    /// </summary>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Entity<TId>)obj);
    }

    /// <summary>
    /// Returns the hash code for this entity based on its ID.
    /// </summary>
    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
