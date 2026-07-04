using System.Text.Json;
using ATBS.Console.Abstractions;
using ATBS.Console.Results;

namespace ATBS.Console.Storage;

/// <summary>
/// Persists collections as JSON files in the local file system.
/// </summary>
public sealed class JsonFileStorage : IFileStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task<Result<IReadOnlyList<T>>> LoadAsync<T>(string path)
    {
        try
        {
            await EnsureFileAsync(path);

            var json = await File.ReadAllTextAsync(path);
        
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException exception)
        {
            return Error.Failure("Storage.InvalidJson", $"Could not read '{path}' because the JSON is invalid: {exception.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Error.Failure("Storage.ReadFailed", $"Could not read '{path}': {exception.Message}");
        }
    }

    public async Task<Result<Success>> SaveAsync<T>(string path, IReadOnlyCollection<T> items)
    {
        try
        {
            await EnsureFileAsync(path);
        
            var json = JsonSerializer.Serialize(items, SerializerOptions);
        
            await File.WriteAllTextAsync(path, json);
            return Result.Success;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or JsonException)
        {
            return Error.Failure("Storage.WriteFailed", $"Could not write '{path}': {exception.Message}");
        }
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