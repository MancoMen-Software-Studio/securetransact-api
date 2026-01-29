using System.Threading;
using System.Threading.Tasks;

namespace SecureTransact.Domain.Abstractions;

/// <summary>
/// Unit of Work pattern interface for coordinating changes across multiple aggregates.
/// Ensures atomicity of operations within a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes to the underlying store atomically.
    /// Also dispatches any domain events raised by the aggregates.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written to the store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
