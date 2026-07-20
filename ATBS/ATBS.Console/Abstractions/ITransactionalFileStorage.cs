using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Loads and saves logical application tables, using the active file transaction when one exists.
/// </summary>
public interface ITransactionalFileStorage
{
    Task<Result<IReadOnlyList<T>>> LoadAsync<T>(TransactionFile file);
    Task<Result<Success>> SaveAsync<T>(TransactionFile file, IReadOnlyCollection<T> items);
}