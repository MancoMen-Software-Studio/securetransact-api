using System;
using System.Linq.Expressions;

namespace SecureTransact.Domain.Specifications;

/// <summary>
/// Base class for specifications with support for logical composition (And, Or, Not).
/// </summary>
/// <typeparam name="T">The type of entity to evaluate.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets the criteria expression for this specification.
    /// </summary>
    public abstract Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Determines whether the specified entity satisfies this specification.
    /// </summary>
    public bool IsSatisfiedBy(T entity)
    {
        Func<T, bool> predicate = Criteria.Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    public Specification<T> And(Specification<T> other) =>
        new AndSpecification<T>(this, other);

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    public Specification<T> Or(Specification<T> other) =>
        new OrSpecification<T>(this, other);

    /// <summary>
    /// Negates this specification.
    /// </summary>
    public Specification<T> Not() =>
        new NotSpecification<T>(this);
}

/// <summary>
/// Combines two specifications using logical AND.
/// </summary>
internal sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> Criteria
    {
        get
        {
            Expression<Func<T, bool>> leftExpr = _left.Criteria;
            Expression<Func<T, bool>> rightExpr = _right.Criteria;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

            BinaryExpression body = Expression.AndAlso(
                Expression.Invoke(leftExpr, parameter),
                Expression.Invoke(rightExpr, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}

/// <summary>
/// Combines two specifications using logical OR.
/// </summary>
internal sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> Criteria
    {
        get
        {
            Expression<Func<T, bool>> leftExpr = _left.Criteria;
            Expression<Func<T, bool>> rightExpr = _right.Criteria;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

            BinaryExpression body = Expression.OrElse(
                Expression.Invoke(leftExpr, parameter),
                Expression.Invoke(rightExpr, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}

/// <summary>
/// Negates a specification.
/// </summary>
internal sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> Criteria
    {
        get
        {
            Expression<Func<T, bool>> expr = _specification.Criteria;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

            UnaryExpression body = Expression.Not(
                Expression.Invoke(expr, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
