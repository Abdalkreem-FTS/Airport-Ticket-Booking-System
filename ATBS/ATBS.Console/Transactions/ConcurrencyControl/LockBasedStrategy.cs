using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions.ConcurrencyControl;

/// <summary>
/// Pessimistic, lock-based concurrency control. Writes always take an
/// exclusive lock held to commit — so <b>every</b> level, including READ UNCOMMITTED and READ
/// COMMITTED, is free of lost updates, exactly as in SQL. What differs between levels is only how
/// reads see data and whether reads take shared locks:
/// <list type="bullet">
/// <item>READ UNCOMMITTED → <see cref="ReadVisibility.Staged"/>, no read locks.</item>
/// <item>READ COMMITTED → <see cref="ReadVisibility.LatestCommitted"/>, no read locks.</item>
/// <item>REPEATABLE READ / SERIALIZABLE → <see cref="ReadVisibility.Pinned"/>, shared read locks held to commit.</item>
/// </list>
/// At table granularity, holding the shared table lock to commit also blocks inserts, so REPEATABLE
/// READ and SERIALIZABLE both prevent phantoms and behave identically here.
/// </summary>
public sealed class LockBasedStrategy(ReadVisibility visibility, bool takeSharedReadLocks) : IConcurrencyControlStrategy
{
    public async Task<string> ReadAsync(ITransactionRuntime runtime, string path, CancellationToken cancellationToken = default)
    {
        if (takeSharedReadLocks)
        {
            await runtime.LockManager.AcquireAsync(runtime.TransactionId, path, LockType.Shared, cancellationToken);
        }

        switch (visibility)
        {
            case ReadVisibility.Pinned:
                
                if (runtime.ReadCache.TryGetValue(path, out var pinned))
                {
                    return pinned;
                }

                var read = await runtime.ReadFromDiskAsync(path, cancellationToken);
                
                runtime.ReadCache[path] = read;
                
                return read;

            case ReadVisibility.Staged:
                
                return runtime.StagedStore.ReadAny(path) ?? await runtime.ReadFromDiskAsync(path, cancellationToken);

            case ReadVisibility.LatestCommitted:
            default:
                
                return await runtime.ReadFromDiskAsync(path, cancellationToken);
        }
    }

    public Task OnWriteAsync(ITransactionRuntime runtime, string path, CancellationToken cancellationToken = default) =>
        runtime.LockManager.AcquireAsync(runtime.TransactionId, path, LockType.Exclusive, cancellationToken);

    public void ValidateAndReserveCommit(ITransactionRuntime runtime, IReadOnlyDictionary<string, ResourceChange> changes) =>
        runtime.VersionStore.CommitLocked(changes);

    public void Release(ITransactionRuntime runtime) => runtime.LockManager.Release(runtime.TransactionId);
}