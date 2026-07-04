using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;

namespace ATBS.Tests;

public sealed class BookingConcurrencyTests
{
    [Fact]
    public async Task Concurrent_Bookings_Never_Oversell_The_Last_Seat()
    {
        using var harness = new TransactionHarness();

        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        harness.SeedJson("passengers.json", [new Passenger { Id = passengerId }]);
        harness.SeedJson("flights.json", [
            new Flight
            {
                Id = flightId,
                Capacity = 1,
                ClassPrices = [new FlightClassPrice { Class = FlightClass.Economy, Price = 100m, AvailableSeats = 1 }]
            }
        ]);

        var request = new CreateBookingRequest { PassengerId = passengerId, FlightId = flightId, Class = FlightClass.Economy };

        // 16 threads race for the single seat.
        var attempts = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => harness.BookingService.BookFlightAsync(request)))
            .ToList();

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(result => result.IsSuccess));

        var flights = await harness.Flights.GetAllAsync();
        Assert.Equal(0, flights.Value.Single().ClassPrices.Single().AvailableSeats);

        var bookings = await harness.Bookings.GetAllAsync();
        Assert.Single(bookings.Value);
    }
}