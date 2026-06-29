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

    public async Task<IEnumerable<T>> LoadAsync<T>(string path)
    {
        await EnsureFileAsync(path);

        var json = await File.ReadAllTextAsync(path);
        
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? [];
    }

    public async Task SaveAsync<T>(string path, IReadOnlyCollection<T> items)
    {
        await EnsureFileAsync(path);
        
        var json = JsonSerializer.Serialize(items, SerializerOptions);
        
        await File.WriteAllTextAsync(path, json);
    }

    private static async Task EnsureFileAsync(string path)
    {
        var directory = Path.GetDirectoryName(path);
        
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, "[]");
        }
    }
}
