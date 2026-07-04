using System.Text.Json;
using ATBS.Console.Abstractions;
using ATBS.Console.Results;
using ATBS.Console.Transactions;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Storage;

/// <summary>
/// Serializes logical tables as JSON and routes writes through the active transaction.
/// </summary>
public sealed class TransactionalJsonFileStorage(
    TransactionFileCatalog fileCatalog,
    FileTransactionContext transactionContext)
    : ITransactionalFileStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task<Result<IReadOnlyList<T>>> LoadAsync<T>(TransactionFile file)
    {
        var path = fileCatalog.GetPath(file);

        try
        {
            var json = transactionContext.Current is { } transaction
                ? await transaction.Read(file)
                : await ReadDirectAsync(path);

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

    public async Task<Result<Success>> SaveAsync<T>(TransactionFile file, IReadOnlyCollection<T> items)
    {
        var path = fileCatalog.GetPath(file);

        try
        {
            EnsureDirectory(path);

            var json = JsonSerializer.Serialize(items, SerializerOptions);
            if (transactionContext.Current is { } transaction)
            {
                await transaction.Write(file, json);
            }
            else
            {
                await File.WriteAllTextAsync(path, json);
            }

            return Result.Success;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or JsonException)
        {
            return Error.Failure("Storage.WriteFailed", $"Could not write '{path}': {exception.Message}");
        }
    }

    private static async Task<string> ReadDirectAsync(string path)
    {
        await EnsureFileAsync(path);
        return await File.ReadAllTextAsync(path);
    }

    private static async Task EnsureFileAsync(string path)
    {
        EnsureDirectory(path);

        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, "[]");
        }
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}