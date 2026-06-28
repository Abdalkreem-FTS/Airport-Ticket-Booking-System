namespace ATBS.Abstractions;

/// <summary>
/// Handles generic loading and saving of persisted application data.
/// </summary>
public interface IFileStorage
{
    IReadOnlyList<T> Load<T>(string path);
    void Save<T>(string path, IReadOnlyCollection<T> items);
}
