using ATBS.Console.Models;
using ATBS.Console.Results;
using ATBS.Tests.TestSupport;

namespace ATBS.Tests.Integration;

/// <summary>
/// The payoff of the SERIALIZABLE file-transaction design: when many bookings race for the same scarce seats,
/// the real lock manager + retry loop must let exactly as many succeed as there are seats — never oversell.
/// A mock-based unit test cannot prove this; only running the actual concurrency-control machinery can.
/// </summary>
public sealed class ConcurrentBookingIntegrationTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(3, 8)]
    public async Task ConcurrentBookings_NeverOversellSeats(int availableSeats, int concurrentAttempts)
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats));

        // Fire all booking attempts at once and let the transaction machinery serialize them.
        var results = await Task.WhenAll(Enumerable
            .Range(0, concurrentAttempts)
            .Select(_ => Task.Run(() => harness.BookAsync(passenger, flight))));

        var succeeded = results.Count(result => result.IsSuccess);
        var failures = results.Where(result => result.IsError).ToList();

        // Exactly `availableSeats` bookings win; the rest are rejected, not silently dropped.
        succeeded.Should().Be(availableSeats);
        failures.Should().HaveCount(concurrentAttempts - availableSeats);

        // Both "out of seats" and "gave up after contention retries" surface as ErrorType.Conflict.
        failures.Should().OnlyContain(result => result.TopError.Type == ErrorType.Conflict);

        // Persisted state agrees with the winners: seats fully drained, one booking row per success.
        (await harness.ReloadFlightAsync(flight)).ClassPrices.Single().AvailableSeats.Should().Be(0);
        (await harness.ReloadBookingsAsync()).Should().HaveCount(availableSeats);
    }

    [Fact]
    public async Task ConcurrentBookings_OnDistinctFlights_AllSucceed()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();

        var flights = new List<Flight>();
        for (var i = 0; i < 5; i++)
        {
            flights.Add(await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 1)));
        }

        Result<Booking>[] results = await Task.WhenAll(
            flights.Select(flight => Task.Run(() => harness.BookAsync(passenger, flight))));

        results.Should().OnlyContain(result => result.IsSuccess);
        (await harness.ReloadBookingsAsync()).Should().HaveCount(flights.Count);
    }
}
