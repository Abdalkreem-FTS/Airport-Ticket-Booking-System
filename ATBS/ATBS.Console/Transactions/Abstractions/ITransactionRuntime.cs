namespace ATBS.Console.Transactions.Abstractions;

/// <summary>
/// The transaction-internal surface a <see cref="IConcurrencyControlStrategy"/> operates on. It
/// exposes just enough of the transaction mechanism (identity, caches, shared managers, disk
/// access) for a strategy to implement read visibility, locking, and commit validation without
/// owning the staging/WAL machinery.
/// </summary>
public interface ITransactionRuntime
{
    Guid TransactionId { get; }

    /// <summary>The global commit sequence captured when this transaction began (used by SNAPSHOT).</summary>
    long SnapshotSequence { get; }

    ILockManager LockManager { get; }

    IVersionStore VersionStore { get; }

    IStagedStore StagedStore { get; }

    /// <summary>Per-resource content this transaction has read or written (its own view). Used for repeatable reads and own-write visibility.</summary>
    IDictionary<string, string> ReadCache { get; }

    /// <summary>Resources this transaction has staged write for (resource path → temp path).</summary>
    IReadOnlyDictionary<string, string> Staged { get; }

    /// <summary>Reads the current committed content directly from disk, or empty string if the file does not exist.</summary>
    Task<string> ReadFromDiskAsync(string path, CancellationToken ct = default);
}