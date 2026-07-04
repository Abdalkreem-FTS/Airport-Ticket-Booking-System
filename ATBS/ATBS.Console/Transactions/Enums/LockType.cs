namespace ATBS.Console.Transactions.Enums;

/// <summary>
/// Lock modes, ordered from weakest to strongest. <see cref="Shared"/> locks are mutually
/// compatible (many readers); <see cref="Exclusive"/> is compatible with nothing.
/// </summary>
public enum LockType
{
    Shared,
    Exclusive
}