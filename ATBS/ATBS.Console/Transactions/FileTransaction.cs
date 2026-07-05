using System.Text;
using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions;

/// <summary>
/// The transaction <i>mechanism</i>: staging via temporary files, a write-ahead log, atomic
/// commit/rollback, and crash-safe durability. Behavior that varies by isolation level (read
/// visibility, locking, commit validation) is delegated to an <see cref="IConcurrencyControlStrategy"/>,
/// so this single class serves every isolation level. Implements <see cref="ITransactionRuntime"/>
/// to expose exactly what the strategy needs.
/// </summary>
public sealed class FileTransaction : ITransactionRuntime, IDisposable
{
    private readonly IConcurrencyControlStrategy _strategy;
    private readonly TransactionFileCatalog _fileCatalog;
    private readonly TransactionLog _transactionLog;

    private readonly Dictionary<string, string> _staged = [];
    private readonly Dictionary<string, string> _readCache = [];
    private readonly Dictionary<string, string> _preImages = [];

    private bool _committed;
    private bool _rolledBack;
    private bool _disposed;

    public FileTransaction(
        IConcurrencyControlStrategy strategy,
        TransactionFileCatalog fileCatalog,
        ILockManager lockManager,
        IVersionStore versionStore,
        IStagedStore stagedStore,
        string transactionLogDirectory,
        long snapshotSequence)
    {
        _strategy = strategy;
        _fileCatalog = fileCatalog;
        LockManager = lockManager;
        VersionStore = versionStore;
        StagedStore = stagedStore;
        SnapshotSequence = snapshotSequence;
        _transactionLog = TransactionLog.Create(transactionLogDirectory, TransactionId);
    }
    
    public Guid TransactionId { get; } = Guid.NewGuid();
    public long SnapshotSequence { get; }
    public ILockManager LockManager { get; }
    public IVersionStore VersionStore { get; }
    public IStagedStore StagedStore { get; }
    public IDictionary<string, string> ReadCache => _readCache;
    public IReadOnlyDictionary<string, string> Staged => _staged;

    public async Task<string> ReadFromDiskAsync(string path, CancellationToken ct = default) =>
        File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : string.Empty;
    
    public async Task<string> Read(TransactionFile file)
    {
        EnsureActive();
        
        var path = ResolvePath(file);

        return _staged.ContainsKey(path) ? _readCache[path] : await _strategy.ReadAsync(this, path);
    }

    public async Task Write(TransactionFile file, string content, CancellationToken ct = default)
    {
        EnsureActive();
        
        var path = ResolvePath(file);

        await _strategy.OnWriteAsync(this, path, ct);

        var temporaryPath = path + $".{TransactionId:N}.temporary";
        
        await using (var stream = new FileStream(
                         path: temporaryPath, 
                         mode: FileMode.Create, 
                         access: FileAccess.Write, 
                         share: FileShare.None, 
                         bufferSize: 4096, // 4096 is the default size; re-write it to edit the options argument
                         options: FileOptions.Asynchronous))
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            
            await stream.WriteAsync(bytes, ct);
            
            await stream.FlushAsync(ct); // .NET buffer → OS
            stream.Flush(flushToDisk: true); // OS → disk
        }
        
        if (!_preImages.ContainsKey(path))
        {
            _preImages[path] = _readCache.TryGetValue(path, out var priorContent)
                ? priorContent
                : await ReadFromDiskAsync(path, ct);
        }

        _staged[path] = temporaryPath;
        StagedStore.Register(path, temporaryPath);

        _readCache[path] = content;

        await _transactionLog.AddEntry(new TransactionLogEntry
        {
            TemporaryPath = temporaryPath,
            FinalPath = path
        });
    }

    public async Task Commit(CancellationToken ct = default)
    {
        EnsureActive();
        
        var changes = new Dictionary<string, ResourceChange>(_staged.Count);
        foreach (var path in _staged.Keys)
        {
            var preImage = _preImages.GetValueOrDefault(path, string.Empty);
            changes[path] = new ResourceChange(preImage, _readCache[path]);
        }

        _strategy.ValidateAndReserveCommit(this, changes);

        await _transactionLog.MarkCommitting(ct);
        _committed = true;

        try
        {
            foreach (var (finalPath, temporaryPath) in _staged)
            {
                MoveIntoPlace(temporaryPath, finalPath);
                StagedStore.Unregister(finalPath, temporaryPath);
            }

            await _transactionLog.MarkCommitted(ct);
        }
        finally
        {
            _strategy.Release(this);
        }

        SafeCleanup();
    }

    public void Rollback(string? reason = null)
    {
        if (_rolledBack || _committed)
        {
            return;
        }

        _transactionLog.MarkRollingBackSync();

        foreach (var (finalPath, temporaryPath) in _staged)
        {
            StagedStore.Unregister(finalPath, temporaryPath);
        }

        _transactionLog.Rollback();
        _strategy.Release(this);
        _rolledBack = true;

        if (reason is not null)
        {
            System.Console.Error.WriteLine($"[Transaction {TransactionId:N}] Rolled back: {reason}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_committed && !_rolledBack)
        {
            Rollback();
        }
    }
    
    private static void MoveIntoPlace(string temporaryPath, string finalPath)
    {
        if (File.Exists(finalPath))
        {
            File.Replace(temporaryPath, finalPath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporaryPath, finalPath);
        }
    }

    private string ResolvePath(TransactionFile file) => _fileCatalog.GetPath(file);

    private void EnsureActive()
    {
        if (_committed)
        {
            throw new InvalidOperationException("Transaction already committed.");
        }

        if (_rolledBack)
        {
            throw new InvalidOperationException("Transaction already rolled back.");
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void SafeCleanup()
    {
        try
        {
            _transactionLog.Cleanup();
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(
                $"[Transaction {TransactionId:N}] Log cleanup failed after successful commit " +
                $"(non-critical — removed on next startup): {ex.Message}");
        }
    }
}