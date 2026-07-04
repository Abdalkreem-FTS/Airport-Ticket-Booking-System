using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Begins file transactions at a chosen isolation level. Locks are acquired lazily and
/// automatically as data is read (shared) and written (exclusive) through the active transaction,
/// so callers no longer declare access up front.
/// </summary>
public interface IFileTransactionFactory
{
    /// <summary>Begins an ambient transaction at the given isolation level; commit it or dispose to roll back.</summary>
    IFileTransactionScope Begin(IsolationLevel isolationLevel);

    /// <summary>
    /// Runs <paramref name="work"/> inside a transaction, committing on success and rolling back on
    /// a business error (an error <see cref="Result{T}"/>). Transient conflicts
    /// (deadlock victims, snapshot write-write conflicts, lock timeouts) are retried automatically
    /// with backoff, mirroring how SQL clients handle deadlock victims.
    /// </summary>
    Task<Result<T>> ExecuteAsync<T>(
        IsolationLevel isolationLevel,
        Func<Task<Result<T>>> work,
        int maxRetries = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>An active, ambient file transaction. Disposing before commit rolls it back.</summary>
public interface IFileTransactionScope : IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    void Rollback(string? reason = null);
}