using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions.ConcurrencyControl;

/// <summary>
/// Maps each <see cref="IsolationLevel"/> to its concurrency-control strategy. Strategies are
/// stateless (all per-transaction state lives in the transaction and the shared managers), so a
/// single instance per level is shared. Adding a new isolation level means adding one entry here.
/// </summary>
public sealed class ConcurrencyControlStrategyFactory
{
    private readonly Dictionary<IsolationLevel, IConcurrencyControlStrategy> _strategies =
        new()
        {
            [IsolationLevel.ReadUncommitted] = new LockBasedStrategy(ReadVisibility.Staged, takeSharedReadLocks: false),
            [IsolationLevel.ReadCommitted] = new LockBasedStrategy(ReadVisibility.LatestCommitted, takeSharedReadLocks: false),
            [IsolationLevel.RepeatableRead] = new LockBasedStrategy(ReadVisibility.Pinned, takeSharedReadLocks: true),
            [IsolationLevel.Serializable] = new LockBasedStrategy(ReadVisibility.Pinned, takeSharedReadLocks: true),
            [IsolationLevel.Snapshot] = new SnapshotStrategy()
        };

    public IConcurrencyControlStrategy For(IsolationLevel level) =>
        _strategies.TryGetValue(level, out var strategy)
            ? strategy
            : throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported isolation level.");

    /// <summary>True for optimistic levels that need a snapshot registered at begin.</summary>
    public static bool RequiresSnapshot(IsolationLevel level) => level == IsolationLevel.Snapshot;
}