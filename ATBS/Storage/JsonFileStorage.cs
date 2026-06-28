using System.Text.Json;
using ATBS.Abstractions;

namespace ATBS.Storage;

/// <summary>
/// Persists collections as JSON files in the local file system.
/// </summary>
public sealed class JsonFileStorage : IFileStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public IReadOnlyList<T> Load<T>(string path)
    {
        EnsureFile(path);

        var json = File.ReadAllText(path);
        
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? [];
    }

    public void Save<T>(string path, IReadOnlyCollection<T> items)
    {
        EnsureFile(path);
        
        var json = JsonSerializer.Serialize(items, SerializerOptions);
        
        File.WriteAllText(path, json);
    }

    private static void EnsureFile(string path)
    {
        var directory = Path.GetDirectoryName(path);
        
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            File.WriteAllText(path, "[]");
        }
    }
}
