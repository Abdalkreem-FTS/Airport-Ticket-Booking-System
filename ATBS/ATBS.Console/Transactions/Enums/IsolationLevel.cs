namespace ATBS.Console.Transactions.Enums;

/// <summary>
/// Isolation levels, mapped to concurrency-control strategies. Locks (where used) are acquired
/// lazily and automatically — shared on read, exclusive on write — and held to commit (strict 2PL).
/// </summary>
public enum IsolationLevel
{
    ReadUncommitted,  // pessimistic writes; reads may see other transactions' staged (dirty) data
    ReadCommitted,    // pessimistic writes; reads see only committed data, fresh on each read
    RepeatableRead,   // shared read locks held to commit; a value read stays stable for the transaction
    Serializable,     // like RepeatableRead; at table granularity the table lock also prevents phantoms
    Snapshot          // optimistic/MVCC: a consistent as-of-begin view; aborts on write-write conflict
}