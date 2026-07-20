using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.Enums;
using ATBS.Console.Transactions.Exceptions;

namespace ATBS.Console.Transactions.Management;

/// <summary>
/// Single-process lock manager with shared/exclusive modes, non-blocking (async) waits, and
/// wait-for-graph deadlock detection. Waiters park on a <see cref="TaskCompletionSource{TResult}"/>
/// instead of spinning, so blocked transactions never occupy a thread-pool thread.
/// </summary>
public sealed class LockManager(int timeoutMs = 10000) : ILockManager
{
    private sealed class Waiter
    {
        public required Guid TransactionId { get; init; }
        public required LockType Mode { get; init; }
        public required TaskCompletionSource<bool> Completion { get; init; }
    }

    private sealed class ResourceLock
    {
        public Dictionary<Guid, LockType> Holders { get; } = [];
        public LinkedList<Waiter> WaitQueue { get; } = [];
    }

    private readonly Dictionary<string, ResourceLock> _resources = []; // each file/table lock
    private readonly Dictionary<Guid, HashSet<string>> _heldByTransaction = []; // each transaction locked resources
    private readonly Dictionary<Guid, HashSet<Guid>> _waitFor = []; // each transaction waiting list - represented as a graph for detecting cycles (deadlocks)
    private readonly Dictionary<Guid, long> _birth = []; // victim choosing helper
    private long _birthCounter;

    private readonly Lock _guard = new();
    
    public async Task AcquireAsync(Guid transactionId, string resource, LockType mode, CancellationToken cancellationToken = default)
    {
        resource = Path.GetFullPath(resource);
        
        Waiter waiter;

        lock (_guard)
        {
            _birth.TryAdd(transactionId, _birthCounter++);

            var resourceLock = GetOrAdd(resource);

            if (TryGrantImmediately(resourceLock, transactionId, mode))
            {
                Track(transactionId, resource);
                
                return; 
            }

            waiter = new Waiter
            {
                TransactionId = transactionId,
                Mode = mode,
                Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
            };
            
            resourceLock.WaitQueue.AddLast(waiter);
            _waitFor[transactionId] = ComputeBlockers(resourceLock, waiter);

            if (DetectDeadlockVictim(transactionId) is { } victim)
            {
                if (victim == transactionId)
                {
                    resourceLock.WaitQueue.Remove(waiter);
                    _waitFor.Remove(transactionId);
                    throw new DeadlockVictimException($"Transaction chosen as deadlock victim while acquiring a {mode} lock on '{Path.GetFileName(resource)}'. Retry the transaction.");
                }

                AbortWaiter(victim);
            }
        }

        try
        {
            await waiter.Completion.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken).ConfigureAwait(false);
            lock (_guard)
            {
                Track(transactionId, resource);
            }
        }
        catch (Exception exception) when (exception is TimeoutException or OperationCanceledException)
        {
            lock (_guard)
            {
                if (waiter.Completion.Task.IsCompletedSuccessfully)
                {
                    Track(transactionId, resource);
                    
                    return;
                }

                RemoveWaiter(resource, waiter);
            }

            if (exception is TimeoutException)
            {
                throw new LockTimeoutException($"Timed out after {timeoutMs}ms acquiring a {mode} lock on '{Path.GetFileName(resource)}'. Another transaction holds a conflicting lock.");
            }

            throw;
        }
    }

    public void Release(Guid transactionId)
    {
        lock (_guard)
        {
            _waitFor.Remove(transactionId);
            _birth.Remove(transactionId);

            if (!_heldByTransaction.Remove(transactionId, out var resources))
            {
                return;
            }

            foreach (var resource in resources)
            {
                if (!_resources.TryGetValue(resource, out var resourceLock))
                {
                    continue;
                }

                resourceLock.Holders.Remove(transactionId);
                ProcessQueue(resource, resourceLock);

                if (resourceLock.Holders.Count == 0 && resourceLock.WaitQueue.Count == 0)
                {
                    _resources.Remove(resource);
                }
            }
        }
    }
    
    private static bool TryGrantImmediately(ResourceLock resourceLock, Guid transactionId, LockType mode)
    {
        if (resourceLock.Holders.TryGetValue(transactionId, out var current))
        {
            if (current >= mode)
            {
                return true;
            }

            if (OthersBlock(resourceLock, transactionId, mode))
            {
                return false;
            }

            resourceLock.Holders[transactionId] = mode;
            
            return true;
        }

        if (OthersBlock(resourceLock, transactionId, mode) || HasForeignWaiter(resourceLock, transactionId))
        {
            return false;
        }

        resourceLock.Holders[transactionId] = mode;
        
        return true;
    }

    private void ProcessQueue(string resource, ResourceLock resourceLock)
    {
        var node = resourceLock.WaitQueue.First;
        while (node is not null)
        {
            var next = node.Next;
            var waiter = node.Value;

            var canGrant = resourceLock.Holders.TryGetValue(waiter.TransactionId, out var held)
                ? held >= waiter.Mode || !OthersBlock(resourceLock, waiter.TransactionId, waiter.Mode)
                : !OthersBlock(resourceLock, waiter.TransactionId, waiter.Mode);

            if (!canGrant)
            {
                break;
            }

            resourceLock.WaitQueue.Remove(node);
            resourceLock.Holders[waiter.TransactionId] = resourceLock.Holders.TryGetValue(waiter.TransactionId, out var prior) && prior > waiter.Mode
                ? prior
                : waiter.Mode;
            Track(waiter.TransactionId, resource);
            _waitFor.Remove(waiter.TransactionId);
            waiter.Completion.TrySetResult(true);

            node = next;
        }
    }

    private static bool OthersBlock(ResourceLock resourceLock, Guid transactionId, LockType mode) =>
        resourceLock.Holders.Any(h => h.Key != transactionId && !Compatible(h.Value, mode));

    private static bool HasForeignWaiter(ResourceLock resourceLock, Guid transactionId) =>
        resourceLock.WaitQueue.Any(w => w.TransactionId != transactionId);

    /// <summary>Compatibility of a requested mode against a mode already held by another transaction.</summary>
    private static bool Compatible(LockType held, LockType requested) => (held, requested) switch
    {
        (LockType.Shared, LockType.Shared) => true,
        _ => false
    };
    
    private static HashSet<Guid> ComputeBlockers(ResourceLock resourceLock, Waiter waiter)
    {
        var blockers = resourceLock.Holders
            .Where(h => h.Key != waiter.TransactionId && !Compatible(h.Value, waiter.Mode))
            .Select(h => h.Key)
            .ToHashSet();

        if (blockers.Count > 0)
        {
            return blockers;
        }

        foreach (var other in resourceLock.WaitQueue.TakeWhile(other => !ReferenceEquals(other, waiter)).Where(other => other.TransactionId != waiter.TransactionId))
        {
            blockers.Add(other.TransactionId);
        }

        return blockers;
    }

    private Guid? DetectDeadlockVictim(Guid start)
    {
        var stack = new List<Guid>();
        var inStack = new HashSet<Guid>();
        Guid? victim = null;

        Dfs(start);
        
        return victim;

        bool Dfs(Guid u)
        {
            stack.Add(u);
            inStack.Add(u);

            if (_waitFor.TryGetValue(u, out var next))
            {
                foreach (var v in next)
                {
                    if (inStack.Contains(v))
                    {
                        var cycle = stack.GetRange(stack.IndexOf(v), stack.Count - stack.IndexOf(v));
                        victim = cycle.OrderByDescending(t => _birth.GetValueOrDefault(t)).First();
                        return true;
                    }

                    if (Dfs(v))
                    {
                        return true;
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            inStack.Remove(u);
            
            return false;
        }
    }

    private void AbortWaiter(Guid transactionId)
    {
        foreach (var resourceLock in _resources.Values)
        {
            var node = resourceLock.WaitQueue.First;
            while (node is not null)
            {
                if (node.Value.TransactionId == transactionId)
                {
                    var toRemove = node;
                    node = node.Next;
                    resourceLock.WaitQueue.Remove(toRemove);
                    toRemove.Value.Completion.TrySetException(new DeadlockVictimException("Transaction chosen as deadlock victim. Retry the transaction."));
                }
                else
                {
                    node = node.Next;
                }
            }
        }

        _waitFor.Remove(transactionId);
    }
    
    private ResourceLock GetOrAdd(string resource)
    {
        if (!_resources.TryGetValue(resource, out var resourceLock))
        {
            _resources[resource] = resourceLock = new ResourceLock();
        }

        return resourceLock;
    }

    private void Track(Guid transactionId, string resource)
    {
        if (!_heldByTransaction.TryGetValue(transactionId, out var set))
        {
            _heldByTransaction[transactionId] = set = [];
        }

        set.Add(Path.GetFullPath(resource));
    }

    private void RemoveWaiter(string resource, Waiter waiter)
    {
        if (_resources.TryGetValue(Path.GetFullPath(resource), out var resourceLock))
        {
            resourceLock.WaitQueue.Remove(waiter);
        }

        _waitFor.Remove(waiter.TransactionId);
    }
}