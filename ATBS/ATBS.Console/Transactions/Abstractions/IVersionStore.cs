namespace ATBS.Console.Transactions.Abstractions;

/// <summary>The before/after content of a resource changed by a committing transaction.</summary>
public readonly record struct ResourceChange(string PreImage, string PostImage);

/// <summary>
/// Multi-version store backing SNAPSHOT isolation. A monotonic global sequence advances on every
/// commit; each committed resource keeps an in-memory version chain (sequence → content). Snapshot
/// reads resolve committed content from this chain — never from disk — so they always see a
/// consistent as-of-begin view, even in the window after a commit has advanced the sequence, but
/// before it has physically replaced the file. Old versions are retained only while an active
/// snapshot could still need them. All members are thread-safe.
/// </summary>
public interface IVersionStore
{
    /// <summary>Registers a new snapshot at the current global sequence and returns that sequence.</summary>
    long BeginSnapshot();

    /// <summary>Releases a snapshot previously registered via <see cref="BeginSnapshot"/>, allowing version GC.</summary>
    void EndSnapshot(long snapshotSequence);

    /// <summary>Returns the committed content of <paramref name="resource"/> as visible to a snapshot taken at <paramref name="snapshotSequence"/>.</summary>
    Task<string> GetSnapshotContentAsync(string resource, long snapshotSequence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically validates and reserves a snapshot commit. Fails (returns false) if any changed
    /// resource was committed by another transaction after <paramref name="snapshotSequence"/>. On
    /// success, appends a new version for each changed resource; the caller then moves the staged files.
    /// </summary>
    bool TryCommitSnapshot(IReadOnlyDictionary<string, ResourceChange> changes, long snapshotSequence);

    /// <summary>Commits resources that were protected by locks (pessimistic path — no validation), appending a new version for each.</summary>
    void CommitLocked(IReadOnlyDictionary<string, ResourceChange> changes);
}
