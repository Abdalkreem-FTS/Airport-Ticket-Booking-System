using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.Exceptions;

namespace ATBS.Console.Transactions.ConcurrencyControl;

/// <summary>
/// Optimistic, multi-version concurrency control (SNAPSHOT isolation). Reads take no locks and see
/// a consistent as-of-begin view served by the injected <see cref="IVersionStore"/>; the snapshot
/// point is captured when the transaction begins. At commit, the write set is validated against
/// concurrent commits — first committer wins, and a losing transaction aborts with a
/// <see cref="TransactionConflictException"/> the caller can retry. Only per-transaction state
/// (the snapshot sequence, the read cache) comes in via <see cref="ITransactionRuntime"/>.
/// </summary>
public sealed class SnapshotStrategy(IVersionStore versionStore) : IConcurrencyControlStrategy
{
    public async Task<string> ReadAsync(ITransactionRuntime runtime, string path, CancellationToken cancellationToken = default)
    {
        if (runtime.ReadCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var content = await versionStore.GetSnapshotContentAsync(path, runtime.SnapshotSequence, cancellationToken);

        runtime.ReadCache[path] = content;

        return content;
    }

    public Task OnWriteAsync(ITransactionRuntime runtime, string resource, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void ValidateAndReserveCommit(ITransactionRuntime runtime, IReadOnlyDictionary<string, ResourceChange> changes)
    {
        if (!versionStore.TryCommitSnapshot(changes, runtime.SnapshotSequence))
        {
            throw new TransactionConflictException(
                "[Snapshot] Write-write conflict: another transaction committed to a written resource after this snapshot began. Retry the transaction.");
        }
    }

    public void Release(ITransactionRuntime runtime) => versionStore.EndSnapshot(runtime.SnapshotSequence);
}
