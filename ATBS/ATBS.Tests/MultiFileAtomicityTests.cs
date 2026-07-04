using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Tests;

/// <summary>
/// A single transaction that writes more than one file must be all-or-nothing across every file: on
/// a business error nothing changes, and on success every file changes together. This proves the WAL
/// multi-file roll-forward, which the single-file recovery tests do not exercise.
/// </summary>
public sealed class MultiFileAtomicityTests
{
    private static readonly Guid FlightId = Guid.NewGuid();

    private static void SeedOneFlightWithFiveSeats(TransactionHarness harness)
    {
        harness.SeedJson("flights.json", [
            new Flight
            {
                Id = FlightId,
                Capacity = 5,
                ClassPrices = [new FlightClassPrice { Class = FlightClass.Economy, Price = 100m, AvailableSeats = 5 }]
            }
        ]);
        harness.SeedJson("bookings.json", Array.Empty<Booking>());
    }

    private static Booking NewBooking() => new()
    {
        PassengerId = Guid.NewGuid(),
        FlightId = FlightId,
        Class = FlightClass.Economy,
        Price = 100m
    };

    /// <summary>Decrements a seat on the flight AND adds a booking, then returns the given result.</summary>
    private static Task<Result<Success>> BookThenReturn(TransactionHarness harness, Result<Success> outcome) =>
        harness.Factory.ExecuteAsync(IsolationLevel.Serializable, async () =>
        {
            var flight = (await harness.Flights.GetByIdAsync(FlightId)).Value;
            flight.ClassPrices.Single().AvailableSeats--;
            var update = await harness.Flights.UpdateAsync(flight);
            if (update.IsError)
            {
                return update.Errors;
            }

            var add = await harness.Bookings.AddAsync(NewBooking());
            if (add.IsError)
            {
                return add.Errors;
            }

            return outcome;
        });

    [Fact]
    public async Task Business_Error_Rolls_Back_Every_File_Touched_By_The_Transaction()
    {
        using var harness = new TransactionHarness();
        SeedOneFlightWithFiveSeats(harness);

        var result = await BookThenReturn(harness, Error.Conflict("Test.Rollback", "force rollback"));

        Assert.True(result.IsError);

        // Both files must look exactly as seeded — the seat write and the booking write both vanish.
        var flights = await harness.Flights.GetAllAsync();
        Assert.Equal(5, flights.Value.Single().ClassPrices.Single().AvailableSeats);

        var bookings = await harness.Bookings.GetAllAsync();
        Assert.Empty(bookings.Value);

        // No staging or log residue left behind.
        Assert.Empty(Directory.GetFiles(harness.DataDirectory, "*.temporary"));
        Assert.Empty(Directory.GetFiles(harness.LogDirectory.DirectoryPath, "*.log"));
    }

    [Fact]
    public async Task Success_Commits_Every_File_Touched_By_The_Transaction_Together()
    {
        using var harness = new TransactionHarness();
        SeedOneFlightWithFiveSeats(harness);

        var result = await BookThenReturn(harness, Result.Success);

        Assert.True(result.IsSuccess);

        var flights = await harness.Flights.GetAllAsync();
        Assert.Equal(4, flights.Value.Single().ClassPrices.Single().AvailableSeats);

        var bookings = await harness.Bookings.GetAllAsync();
        Assert.Single(bookings.Value);

        Assert.Empty(Directory.GetFiles(harness.DataDirectory, "*.temporary"));
        Assert.Empty(Directory.GetFiles(harness.LogDirectory.DirectoryPath, "*.log"));
    }
}