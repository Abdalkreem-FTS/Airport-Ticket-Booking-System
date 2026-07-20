namespace ATBS.Console.Transactions.Exceptions;

/// <summary>
/// Thrown when a lock cannot be acquired within the configured timeout. Transient — the caller may
/// retry (mirrors a SQL lock-wait timeout).
/// </summary>
public sealed class LockTimeoutException(string message) : TransactionConflictException(message);