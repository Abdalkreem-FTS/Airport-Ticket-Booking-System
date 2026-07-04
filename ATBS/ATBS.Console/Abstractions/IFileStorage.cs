using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Handles generic loading and saving of persisted application data.
/// </summary>
public interface IFileStorage
{
    Task<Result<IReadOnlyList<T>>> LoadAsync<T>(string path);
    Task<Result<Success>> SaveAsync<T>(string path, IReadOnlyCollection<T> items);
}