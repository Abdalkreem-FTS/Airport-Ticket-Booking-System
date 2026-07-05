namespace ATBS.Console.Transactions.Abstractions;

/// <summary>
/// The transaction-internal surface a <see cref="IConcurrencyControlStrategy"/> operates on. It
/// exposes only the <b>per-transaction</b> state a strategy needs (identity, snapshot point, caches,
/// raw disk access). The shared coordinators (lock manager, version store, staged store) are process
/// singletons and are injected into the strategies directly, so they are deliberately <i>not</i> part
/// of this per-operation surface.
/// </summary>
public interface ITransactionRuntime
{
    Guid TransactionId { get; }

    /// <summary>The global commit sequence captured when this transaction began (used by SNAPSHOT).</summary>
    long SnapshotSequence { get; }

    /// <summary>Per-resource content this transaction has read or written (its own view). Used for repeatable reads and own-write visibility.</summary>
    IDictionary<string, string> ReadCache { get; }

    /// <summary>Resources this transaction has staged write for (resource path → temp path).</summary>
    IReadOnlyDictionary<string, string> Staged { get; }

    /// <summary>Reads the current committed content directly from disk, or empty string if the file does not exist.</summary>
    Task<string> ReadFromDiskAsync(string path, CancellationToken ct = default);
}
