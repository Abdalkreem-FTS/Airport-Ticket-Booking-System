using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Tests;

/// <summary>
/// End-to-end proof that a genuine deadlock is survived by the whole stack — not just detected by the
/// lock manager in isolation. Two transactions grab the two files in opposite order to force a cycle;
/// the manager aborts one as the victim, and <c>ExecuteAsync</c> transparently retries it so BOTH
/// units of work still succeed.
/// </summary>
public sealed class DeadlockRetryTests
{
    private static Booking NewBooking() => new()
    {
        PassengerId = Guid.NewGuid(),
        FlightId = Guid.NewGuid(),
        Class = FlightClass.Economy,
        Price = 100m
    };

    [Fact]
    public async Task Deadlocked_Transactions_Are_Retried_And_Both_Eventually_Commit()
    {
        using var harness = new TransactionHarness(lockTimeoutMs: 5000);
        harness.SeedJson("flights.json", Array.Empty<Flight>());
        harness.SeedJson("bookings.json", Array.Empty<Booking>());

        // Gates that let each transaction take its FIRST exclusive lock before either asks for its
        // second — that ordering is what guarantees a cycle. On a retry the gates are already set, so
        // the retried transaction runs straight through without deadlocking again.
        var t1TookFlights = new TaskCompletionSource();
        var t2TookBookings = new TaskCompletionSource();

        // T1: write flights first, then bookings.
        var work1 = Task.Run(() => harness.Factory.ExecuteAsync<Created>(IsolationLevel.Serializable, async () =>
        {
            var first = await harness.Flights.AddAsync(new Flight { Capacity = 1 });
            if (first.IsError)
            {
                return first.Errors;
            }

            t1TookFlights.TrySetResult();
            await t2TookBookings.Task;

            var second = await harness.Bookings.AddAsync(NewBooking());
            return second.IsError ? second.Errors : Result.Created;
        }));

        // T2: write bookings first, then flights — the opposite order.
        var work2 = Task.Run(() => harness.Factory.ExecuteAsync<Created>(IsolationLevel.Serializable, async () =>
        {
            var first = await harness.Bookings.AddAsync(NewBooking());
            if (first.IsError)
            {
                return first.Errors;
            }

            t2TookBookings.TrySetResult();
            await t1TookFlights.Task;

            var second = await harness.Flights.AddAsync(new Flight { Capacity = 1 });
            return second.IsError ? second.Errors : Result.Created;
        }));

        var results = await Task.WhenAll(work1, work2).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.All(results, result => Assert.True(result.IsSuccess,
            result.IsError ? result.TopError.Description : string.Empty));

        // Each transaction added exactly one of each, so both files end up with two rows.
        Assert.Equal(2, (await harness.Flights.GetAllAsync()).Value.Count);
        Assert.Equal(2, (await harness.Bookings.GetAllAsync()).Value.Count);

        Assert.Empty(Directory.GetFiles(harness.DataDirectory, "*.temporary"));
        Assert.Empty(Directory.GetFiles(harness.LogDirectory.DirectoryPath, "*.log"));
    }
}