using ATBS.Console.Transactions.Enums;
using ATBS.Console.Transactions.Exceptions;
using ATBS.Console.Transactions.Management;

namespace ATBS.Tests;

public sealed class LockManagerTests
{
    private static string Resource(string name) => Path.Combine(Path.GetTempPath(), "atbs_lock_" + name);

    [Fact]
    public async Task Shared_Locks_Are_Granted_Concurrently()
    {
        var lockManager = new LockManager();
        var resource = Resource(Guid.NewGuid().ToString("N"));

        await lockManager.AcquireAsync(Guid.NewGuid(), resource, LockType.Shared);
        var secondReader = lockManager.AcquireAsync(Guid.NewGuid(), resource, LockType.Shared);

        Assert.True(secondReader.IsCompletedSuccessfully || await CompletesQuickly(secondReader));
    }

    [Fact]
    public async Task Exclusive_Lock_Blocks_Until_Released()
    {
        var lockManager = new LockManager();
        var resource = Resource(Guid.NewGuid().ToString("N"));
        var holder = Guid.NewGuid();
        var waiter = Guid.NewGuid();

        await lockManager.AcquireAsync(holder, resource, LockType.Exclusive);
        var blocked = lockManager.AcquireAsync(waiter, resource, LockType.Exclusive);

        await Task.Delay(100);
        Assert.False(blocked.IsCompleted);

        lockManager.Release(holder);
        await blocked.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Deadlock_Is_Detected_And_The_Youngest_Transaction_Is_Aborted()
    {
        var lockManager = new LockManager(timeoutMs: 5000);
        var a = Resource("A_" + Guid.NewGuid().ToString("N"));
        var b = Resource("B_" + Guid.NewGuid().ToString("N"));

        var older = Guid.NewGuid();
        var younger = Guid.NewGuid();

        await lockManager.AcquireAsync(older, a, LockType.Exclusive);
        await lockManager.AcquireAsync(younger, b, LockType.Exclusive);

        var olderWaitsForB = lockManager.AcquireAsync(older, b, LockType.Exclusive);
        await Task.Delay(50);
        Assert.False(olderWaitsForB.IsCompleted);

        await Assert.ThrowsAsync<DeadlockVictimException>(
            () => lockManager.AcquireAsync(younger, a, LockType.Exclusive));

        lockManager.Release(younger);
        await olderWaitsForB.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Lock_Wait_Times_Out_When_Never_ranted()
    {
        var lockManager = new LockManager(timeoutMs: 150);
        var resource = Resource(Guid.NewGuid().ToString("N"));

        await lockManager.AcquireAsync(Guid.NewGuid(), resource, LockType.Exclusive);

        await Assert.ThrowsAsync<LockTimeoutException>(
            () => lockManager.AcquireAsync(Guid.NewGuid(), resource, LockType.Exclusive));
    }

    private static async Task<bool> CompletesQuickly(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(500));
        return completed == task;
    }
}