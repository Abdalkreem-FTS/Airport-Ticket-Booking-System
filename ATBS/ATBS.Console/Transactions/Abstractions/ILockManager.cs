using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions.Abstractions;

/// <summary>
/// A single-process lock manager providing shared/exclusive locks per resource with
/// non-blocking (async) waits and wait-for-graph deadlock detection. Locks are held until the
/// owning transaction releases them all (strict two-phase locking).
/// </summary>
public interface ILockManager
{
    /// <summary>
    /// Acquires <paramref name="mode"/> on <paramref name="resource"/> for the transaction,
    /// waiting asynchronously if it conflicts. Re-entrant: re-requesting an equal-or-weaker mode
    /// already held returns immediately, and a Shared lock is upgraded to Exclusive in place when compatible.
    /// </summary>
    /// <exception cref="DeadlockVictimException">This transaction was chosen to break a deadlock cycle.</exception>
    /// <exception cref="LockTimeoutException">The lock could not be acquired before the timeout.</exception>
    Task AcquireAsync(Guid transactionId, string resource, LockType mode, CancellationToken cancellationToken = default);

    /// <summary>Releases every lock held by the transaction and wakes any now-grantable waiters.</summary>
    void Release(Guid transactionId);
}