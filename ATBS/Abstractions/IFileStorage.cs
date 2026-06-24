namespace ATBS.Abstractions;

public interface IFileStorage
{
    IReadOnlyList<T> Load<T>(string path);
    void Save<T>(string path, IReadOnlyCollection<T> items);
}
