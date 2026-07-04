using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions;

public class TransactionLogEntry
{
    public string TemporaryPath { get; init; } = string.Empty;
    public string FinalPath { get; init; } = string.Empty;
}

public class TransactionLog
{
    public Guid TransactionId { get; init; }
    public TransactionLogStatus Status { get; set; } = TransactionLogStatus.Pending;
    public List<TransactionLogEntry> Entries { get; init; } = [];

    [JsonIgnore]
    private string? _path;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };


    public static TransactionLog Create(string transactionLogDirectory, Guid transactionId)
    {
        Directory.CreateDirectory(transactionLogDirectory);
        
        return new TransactionLog
        {
            TransactionId = transactionId,
            _path = Path.Combine(transactionLogDirectory, $"{transactionId}.log")
        };
    }

    public static async Task<TransactionLog?> Load(string path)
    {
        try
        {
            var log = JsonSerializer.Deserialize<TransactionLog>(await File.ReadAllTextAsync(path), JsonOptions);
            log?._path = path;

            return log;
        }
        catch
        {
            return null;
        }
    }
    
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (_path is null)
        {
            return;
        }

        var temporaryPath = _path + ".temporary";
        
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, JsonOptions));

        await using (var stream = new FileStream(
                         path: temporaryPath, 
                         mode: FileMode.Create, 
                         access: FileAccess.Write, 
                         share: FileShare.None, 
                         bufferSize: 4096, 
                         options: FileOptions.Asynchronous))
        {
            await stream.WriteAsync(bytes, ct);
            
            await stream.FlushAsync(ct);
            stream.Flush(flushToDisk: true);
        }

        File.Move(temporaryPath, _path, overwrite: true);
    }

    private void SaveSync()
    {
        if (_path is null)
        {
            return;
        }

        var temporaryPath = _path + ".temporary";
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, JsonOptions));

        using (var fs = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write,
                                       FileShare.None, 4096, FileOptions.WriteThrough))
        {
            fs.Write(bytes);
            
            fs.Flush(flushToDisk: true);
        }

        File.Move(temporaryPath, _path, overwrite: true);
    }
    
    public async Task AddEntry(TransactionLogEntry entry)
    {
        Entries.Add(entry);
        await SaveAsync();
    }

    public async Task MarkCommitting(CancellationToken ct = default)
    {
        Status = TransactionLogStatus.Committing;
        await SaveAsync(ct);
    }

    public async Task MarkCommitted(CancellationToken ct = default)
    {
        Status = TransactionLogStatus.Committed;
        await SaveAsync(ct);
    }
    
    public void MarkRollingBackSync()
    {
        Status = TransactionLogStatus.RollingBack;
        SaveSync();
    }
    
    public void ReplayCommit()
    {
        foreach (var entry in Entries.Where(e => File.Exists(e.TemporaryPath)))
        {
            File.Move(entry.TemporaryPath, entry.FinalPath, overwrite: true);
        }
    }
    
    public void Rollback()
    {
        // The originals are never overwritten before commit, so undoing a transaction
        // is simply discarding its staged temp files — there is nothing to restore.
        Cleanup();
    }

    public void Cleanup()
    {
        foreach (var entry in Entries)
        {
            TryDelete(entry.TemporaryPath);
        }

        if (_path is not null)
        {
            TryDelete(_path);
        }
    }

    private static void TryDelete(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
             // TransactionRecovery retries on next startup
        }
    }
}