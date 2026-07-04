namespace ATBS.Console.Transactions.Exceptions;

/// <summary>
/// Thrown when a transaction is chosen as the victim to break a deadlock cycle. Like a SQL
/// deadlock-victim error (SQLSTATE 40P01 / SQL Server 1205), this is a transient failure: the
/// caller may safely retry the transaction from the beginning.
/// </summary>
public sealed class DeadlockVictimException(string message) : TransactionConflictException(message);