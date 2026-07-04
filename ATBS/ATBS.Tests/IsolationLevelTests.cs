using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Tests;

public sealed class IsolationLevelTests
{
    private static Booking NewBooking() => new()
    {
        PassengerId = Guid.NewGuid(),
        FlightId = Guid.NewGuid(),
        Class = FlightClass.Economy,
        Price = 100m
    };

    private static Task<Result<Created>> AddOneBooking(TransactionHarness harness, IsolationLevel level) =>
        harness.Factory.ExecuteAsync<Created>(level, async () =>
        {
            var result = await harness.Bookings.AddAsync(NewBooking());
            return result.IsError ? result.Errors : Result.Created;
        });

    [Fact]
    public async Task Serializable_Prevents_Lost_Updates_Under_Concurrent_Read_Modify_Write()
    {
        using var harness = new TransactionHarness();
        harness.SeedJson("bookings.json", Array.Empty<Booking>());

        var t1 = Task.Run(() => AddOneBooking(harness, IsolationLevel.Serializable));
        var t2 = Task.Run(() => AddOneBooking(harness, IsolationLevel.Serializable));
        var r1 = await t1;
        var r2 = await t2;

        Assert.True(r1.IsSuccess, $"t1 failed: {(r1.IsError ? r1.TopError.Description : "")}");
        Assert.True(r2.IsSuccess, $"t2 failed: {(r2.IsError ? r2.TopError.Description : "")}");

        var bookings = await harness.Bookings.GetAllAsync();
        Assert.Equal(2, bookings.Value.Count);
    }

    [Fact]
    public async Task Snapshot_Prevents_Lost_Updates_By_Aborting_And_etrying_The_Conflict()
    {
        using var harness = new TransactionHarness();
        harness.SeedJson("bookings.json", Array.Empty<Booking>());

        await Task.WhenAll(
            Task.Run(() => AddOneBooking(harness, IsolationLevel.Snapshot)),
            Task.Run(() => AddOneBooking(harness, IsolationLevel.Snapshot)));

        var bookings = await harness.Bookings.GetAllAsync();
        Assert.Equal(2, bookings.Value.Count);
    }

    [Fact]
    public async Task ReadUncommitted_Sees_Dirty_Writes_That_ReadCommitted_Does_Not()
    {
        using var harness = new TransactionHarness();
        harness.SeedJson("bookings.json", Array.Empty<Booking>());

        var writerHasStaged = new TaskCompletionSource();
        var writerMayCommit = new TaskCompletionSource();

        var writer = Task.Run(async () =>
        {
            using var transaction = harness.Factory.Begin(IsolationLevel.ReadCommitted);
            await harness.Bookings.AddAsync(NewBooking());
            writerHasStaged.SetResult();
            await writerMayCommit.Task;
            await transaction.CommitAsync();
        });

        await writerHasStaged.Task;

        var dirtyCount = await Task.Run(async () =>
        {
            using var transaction = harness.Factory.Begin(IsolationLevel.ReadUncommitted);
            return (await harness.Bookings.GetAllAsync()).Value.Count;
        });

        var committedCount = await Task.Run(async () =>
        {
            using var transaction = harness.Factory.Begin(IsolationLevel.ReadCommitted);
            return (await harness.Bookings.GetAllAsync()).Value.Count;
        });

        writerMayCommit.SetResult();
        await writer;

        Assert.Equal(1, dirtyCount);
        Assert.Equal(0, committedCount);
    }

    [Fact]
    public async Task RepeatableRead_Pins_Its_Value_And_Blocks_A_Concurrent_Writer_Until_It_Commits()
    {
        using var harness = new TransactionHarness(lockTimeoutMs: 5000);
        harness.SeedJson("bookings.json", new[] { NewBooking() });

        var readerReadOnce = new TaskCompletionSource();
        var readerMayReadAgain = new TaskCompletionSource();
        var secondCount = new TaskCompletionSource<int>();

        // Reader keeps a REPEATABLE READ transaction open, holding a shared lock to commit.
        var reader = Task.Run(async () =>
        {
            using var transaction = harness.Factory.Begin(IsolationLevel.RepeatableRead);
            var firstCount = (await harness.Bookings.GetAllAsync()).Value.Count;
            readerReadOnce.SetResult();

            await readerMayReadAgain.Task;
            secondCount.SetResult((await harness.Bookings.GetAllAsync()).Value.Count);

            await transaction.CommitAsync();
            return firstCount;
        });

        await readerReadOnce.Task;

        // Writer tries to add a booking; its exclusive lock conflicts with the reader's shared lock,
        // so it cannot commit while the reader is still open.
        var writerCommitted = new TaskCompletionSource();
        var writer = Task.Run(async () =>
        {
            var result = await harness.Factory.ExecuteAsync<Created>(IsolationLevel.ReadCommitted, async () =>
            {
                var add = await harness.Bookings.AddAsync(NewBooking());
                return add.IsError ? add.Errors : Result.Created;
            });
            writerCommitted.SetResult();
            return result;
        });

        // Give the writer time to reach — and block on — the exclusive lock.
        await Task.Delay(300);
        Assert.False(writerCommitted.Task.IsCompleted, "writer committed while the reader still held a shared lock");

        // Reader's second read still sees the pinned value, even though a writer is queued.
        readerMayReadAgain.SetResult();
        Assert.Equal(1, await secondCount.Task);

        var firstCount = await reader;               // reader commits, releasing its shared lock
        var writerResult = await writer.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, firstCount);
        Assert.True(writerResult.IsSuccess);
        Assert.Equal(2, (await harness.Bookings.GetAllAsync()).Value.Count);
    }

    [Fact]
    public async Task ReadCommitted_Sees_A_Newly_Committed_Value_Between_Two_Reads()
    {
        using var harness = new TransactionHarness();
        harness.SeedJson("bookings.json", new[] { NewBooking() });

        var readerReadOnce = new TaskCompletionSource();
        var writerCommitted = new TaskCompletionSource();
        var counts = new TaskCompletionSource<(int First, int Second)>();

        // Reader takes no read locks, so a concurrent writer can commit freely between its two reads.
        var reader = Task.Run(async () =>
        {
            using var transaction = harness.Factory.Begin(IsolationLevel.ReadCommitted);
            var first = (await harness.Bookings.GetAllAsync()).Value.Count;
            readerReadOnce.SetResult();

            await writerCommitted.Task;
            var second = (await harness.Bookings.GetAllAsync()).Value.Count;
            counts.SetResult((first, second));

            await transaction.CommitAsync();
        });

        await readerReadOnce.Task;

        var writerResult = await Task.Run(() => harness.Factory.ExecuteAsync<Created>(IsolationLevel.ReadCommitted, async () =>
        {
            var add = await harness.Bookings.AddAsync(NewBooking());
            return add.IsError ? add.Errors : Result.Created;
        }));
        Assert.True(writerResult.IsSuccess);
        writerCommitted.SetResult();

        await reader;
        var (first, second) = await counts.Task;

        Assert.Equal(1, first);
        Assert.Equal(2, second);
    }
}