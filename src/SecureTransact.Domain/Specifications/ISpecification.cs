using System;
using System.Linq.Expressions;

namespace SecureTransact.Domain.Specifications;

/// <summary>
/// Defines a specification pattern for composable business rules.
/// </summary>
/// <typeparam name="T">The type of entity to evaluate.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the criteria expression for this specification.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Determines whether the specified entity satisfies this specification.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
    bool IsSatisfiedBy(T entity);
}
