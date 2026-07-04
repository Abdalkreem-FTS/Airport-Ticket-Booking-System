namespace ATBS.Console.Transactions.Exceptions;

/// <summary>Thrown by Snapshot transactions when a write-write conflict is detected.</summary>
public class TransactionConflictException(string message) : TransactionException(message);