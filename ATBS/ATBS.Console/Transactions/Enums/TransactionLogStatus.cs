namespace ATBS.Console.Transactions.Enums;

public enum TransactionLogStatus
{
    Pending, 
    Committing, 
    Committed,
    RollingBack,
    RolledBack,
}