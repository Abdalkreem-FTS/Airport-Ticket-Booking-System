using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions.ConcurrencyControl;

/// <summary>
/// Maps each <see cref="IsolationLevel"/> to its concurrency-control strategy. Strategies are
/// stateless with respect to per-transaction state (that arrives per call via
/// <see cref="ITransactionRuntime"/>); the shared coordinators they depend on are process singletons
/// injected here and handed to each strategy once. A single strategy instance per level is shared.
/// Adding a new isolation level means adding one entry here.
/// </summary>
public sealed class ConcurrencyControlStrategyFactory(
    ILockManager lockManager,
    IVersionStore versionStore,
    IStagedStore stagedStore)
{
    private readonly Dictionary<IsolationLevel, IConcurrencyControlStrategy> _strategies = new()
    {
        [IsolationLevel.ReadUncommitted] = new LockBasedStrategy(lockManager, stagedStore, versionStore, ReadVisibility.Staged, takeSharedReadLocks: false),
        [IsolationLevel.ReadCommitted] = new LockBasedStrategy(lockManager, stagedStore, versionStore, ReadVisibility.LatestCommitted, takeSharedReadLocks: false),
        [IsolationLevel.RepeatableRead] = new LockBasedStrategy(lockManager, stagedStore, versionStore, ReadVisibility.Pinned, takeSharedReadLocks: true),
        [IsolationLevel.Serializable] = new LockBasedStrategy(lockManager, stagedStore, versionStore, ReadVisibility.Pinned, takeSharedReadLocks: true),
        [IsolationLevel.Snapshot] = new SnapshotStrategy(versionStore)
    };

    public IConcurrencyControlStrategy For(IsolationLevel level) =>
        _strategies.TryGetValue(level, out var strategy)
            ? strategy
            : throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported isolation level.");

    /// <summary>True for optimistic levels that need a snapshot registered at begin.</summary>
    public static bool RequiresSnapshot(IsolationLevel level) => level == IsolationLevel.Snapshot;
}
