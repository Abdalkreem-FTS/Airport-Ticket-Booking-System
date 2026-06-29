namespace ATBS.Abstractions;

/// <summary>
/// Handles generic loading and saving of persisted application data.
/// </summary>
public interface IFileStorage
{
    Task<IEnumerable<T>> LoadAsync<T>(string path);
    Task SaveAsync<T>(string path, IReadOnlyCollection<T> items);
}
