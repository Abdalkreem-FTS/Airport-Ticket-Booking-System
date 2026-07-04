using ATBS.Console.Abstractions;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.ConcurrencyControl;
using ATBS.Console.Transactions.Enums;
using ATBS.Console.Transactions.Exceptions;

namespace ATBS.Console.Transactions;

public sealed class FileTransactionFactory(
    TransactionFileCatalog fileCatalog,
    FileTransactionContext transactionContext,
    TransactionLogDirectory transactionLogDirectory,
    ConcurrencyControlStrategyFactory strategyFactory,
    ILockManager lockManager,
    IVersionStore versionStore,
    IStagedStore stagedStore)
    : IFileTransactionFactory
{
    private const int BaseBackoffMs = 10;
    private const int MaxBackoffMs = 250;

    public IFileTransactionScope Begin(IsolationLevel isolationLevel)
    {
        if (transactionContext.Current is not null)
        {
            throw new InvalidOperationException("A file transaction is already active for this operation (nested transactions are not supported).");
        }

        var snapshotSequence = ConcurrencyControlStrategyFactory.RequiresSnapshot(isolationLevel)
            ? versionStore.BeginSnapshot()
            : 0L;

        var transaction = new FileTransaction(
            strategyFactory.For(isolationLevel),
            fileCatalog,
            lockManager,
            versionStore,
            stagedStore,
            transactionLogDirectory.DirectoryPath,
            snapshotSequence);

        return new FileTransactionScope(transactionContext, transaction);
    }

    public async Task<Result<T>> ExecuteAsync<T>(
        IsolationLevel isolationLevel,
        Func<Task<Result<T>>> work,
        int maxRetries = 5,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            var scope = Begin(isolationLevel);
            Result<T> result;

            try
            {
                result = await work();
                if (result.IsSuccess)
                {
                    await scope.CommitAsync(cancellationToken);
                }
            }
            catch (TransactionConflictException exception)
            {
                scope.Dispose();

                if (attempt >= maxRetries)
                {
                    return Error.Conflict("Transactions.Conflict",
                        $"Transaction aborted after {maxRetries + 1} attempts due to contention: {exception.Message}");
                }

                await Task.Delay(BackoffMs(attempt), cancellationToken);
                
                continue;
            }
            catch
            {
                scope.Dispose();
                
                throw;
            }

            scope.Dispose();
            
            return result;
        }
    }

    private static int BackoffMs(int attempt)
    {
        var exponential = Math.Min(MaxBackoffMs, BaseBackoffMs * (1 << Math.Min(attempt, 8)));
        return Random.Shared.Next(BaseBackoffMs, exponential + 1); 
    }

    private sealed class FileTransactionScope : IFileTransactionScope
    {
        private readonly FileTransactionContext _transactionContext;
        private readonly FileTransaction _transaction;
        private readonly FileTransaction? _previous;
        private bool _completed;
        private bool _disposed;

        public FileTransactionScope(FileTransactionContext transactionContext, FileTransaction transaction)
        {
            _transactionContext = transactionContext;
            _transaction = transaction;
            _previous = transactionContext.Current;
            _transactionContext.Current = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.Commit(cancellationToken);
            _completed = true;
        }

        public void Rollback(string? reason = null)
        {
            _transaction.Rollback(reason);
            _completed = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (!_completed)
            {
                _transaction.Rollback();
            }

            _transaction.Dispose();
            _transactionContext.Current = _previous;
        }
    }
}