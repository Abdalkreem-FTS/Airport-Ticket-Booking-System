namespace ATBS.Console.Transactions.Enums;

/// <summary>How a lock-based transaction resolves the committed value it reads.</summary>
public enum ReadVisibility
{
    /// <summary>READ UNCOMMITTED: return another transaction's staged (dirty) write if present, else the committed value.</summary>
    Staged,

    /// <summary>READ COMMITTED: read the latest committed value fresh on every read (re-reads may differ).</summary>
    LatestCommitted,

    /// <summary>REPEATABLE READ / SERIALIZABLE: read once, then pin that value for the rest of the transaction (a shared lock keeps it stable).</summary>
    Pinned
}