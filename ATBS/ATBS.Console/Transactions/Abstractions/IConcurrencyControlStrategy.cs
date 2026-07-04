using ATBS.Console.Transactions.Exceptions;

namespace ATBS.Console.Transactions.Abstractions;

/// <summary>
/// Encapsulates the concurrency-control policy for one isolation level: how reads see data, what
/// locks (if any) reads and writes take, and how a commit is validated. The transaction mechanism
/// (staging, WAL, atomic rename, recovery) is identical across levels and lives in the transaction
/// itself — only this policy varies, so a new isolation level is a new strategy, not a new
/// transaction subclass.
/// </summary>
public interface IConcurrencyControlStrategy
{
    /// <summary>Resolves the content this transaction should see for <paramref name="resource"/>, acquiring read locks if the policy requires them.</summary>
    Task<string> ReadAsync(ITransactionRuntime runtime, string resource, CancellationToken cancellationToken = default);

    /// <summary>Invoked before write is staged, to acquire write locks (pessimistic) or record write intent (optimistic).</summary>
    Task OnWriteAsync(ITransactionRuntime runtime, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and, for optimistic policies, atomically reserves the commit against concurrent
    /// writers, advancing the version store. Called after the change set is captured but before any
    /// file is moved. Throws <see cref="TransactionConflictException"/> on conflict.
    /// </summary>
    void ValidateAndReserveCommit(ITransactionRuntime runtime, IReadOnlyDictionary<string, ResourceChange> changes);

    /// <summary>Releases any resources (locks, snapshot registration) held by the transaction.</summary>
    void Release(ITransactionRuntime runtime);
}