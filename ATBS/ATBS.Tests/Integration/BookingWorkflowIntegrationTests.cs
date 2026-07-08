using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Tests.TestSupport;

namespace ATBS.Tests.Integration;

/// <summary>
/// End-to-end booking flows exercised through the real service graph: transaction factory, repositories,
/// and JSON file storage. Every assertion re-reads state from storage, so we check what was actually
/// committed to disk, not just what the service returned.
/// </summary>
public sealed class BookingWorkflowIntegrationTests
{
    [Fact]
    public async Task BookFlight_DecrementsSeat_AndPersistsBooking()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 3, price: 250m));

        var result = await harness.BookAsync(passenger, flight);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, (await harness.ReloadFlightAsync(flight)).ClassPrices.Single().AvailableSeats);

        var booking = Assert.Single(await harness.ReloadBookingsAsync());
        Assert.Equal(passenger.Id, booking.PassengerId);
        Assert.Equal(flight.Id, booking.FlightId);
        Assert.Equal(250m, booking.Price);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task BookFlight_CommitsCleanly_LeavingNoPendingTransactionLogs()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 5));

        Assert.True((await harness.BookAsync(passenger, flight)).IsSuccess);

        // A committed transaction cleans up its write-ahead log, so nothing is left for crash recovery to replay.
        Assert.Empty(harness.PendingTransactionLogFiles);
    }

    [Fact]
    public async Task BookThenCancel_ReleasesSeat_AndMarksBookingCancelled()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 4));

        var booked = await harness.BookAsync(passenger, flight);
        Assert.True(booked.IsSuccess);

        var cancelled = await harness.BookingService.CancelBookingAsync(passenger.Id, booked.Value.Id);

        Assert.True(cancelled.IsSuccess);
        Assert.Equal(4, (await harness.ReloadFlightAsync(flight)).ClassPrices.Single().AvailableSeats);
        Assert.Equal(BookingStatus.Cancelled, Assert.Single(await harness.ReloadBookingsAsync()).Status);
    }

    [Fact]
    public async Task ModifyBooking_MovesSeatBetweenClasses_AndRepricesOnDisk()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewFlight(classPrices:
        [
            Builders.NewClassPrice(FlightClass.Economy, price: 100m, availableSeats: 5),
            Builders.NewClassPrice(FlightClass.Business, price: 300m, availableSeats: 2)
        ]));

        var booked = await harness.BookAsync(passenger, flight);
        Assert.True(booked.IsSuccess);

        var modified = await harness.BookingService.ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passenger.Id,
            BookingId = booked.Value.Id,
            NewClass = FlightClass.Business
        });

        Assert.True(modified.IsSuccess);
        Assert.Equal(FlightClass.Business, modified.Value.Class);
        Assert.Equal(300m, modified.Value.Price);

        var persistedFlight = await harness.ReloadFlightAsync(flight);
        // Economy: 5 - 1 (booking) + 1 (moved out) = 5. Business: 2 - 1 (moved in) = 1.
        Assert.Equal(5, Seats(persistedFlight, FlightClass.Economy));
        Assert.Equal(1, Seats(persistedFlight, FlightClass.Business));

        var booking = Assert.Single(await harness.ReloadBookingsAsync());
        Assert.Equal(FlightClass.Business, booking.Class);
        Assert.Equal(300m, booking.Price);
    }

    [Fact]
    public async Task ManagerFilter_SeesBookingsCreatedThroughTheBookingService()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewFlight(departureCountry: "Jordan",
            classPrices: [Builders.NewClassPrice(FlightClass.Economy, price: 250m, availableSeats: 3)]));

        await harness.BookAsync(passenger, flight);

        // A different service reads the same persisted files and joins bookings to their flight.
        var filtered = await harness.ManagerBookingService.FilterBookingsAsync(new BookingSearchCriteria
        {
            DepartureCountry = "Jordan"
        });

        Assert.True(filtered.IsSuccess);
        Assert.Equal(flight.Id, Assert.Single(filtered.Value).FlightId);
    }

    private static int Seats(Flight flight, FlightClass flightClass) =>
        flight.ClassPrices.Single(price => price.Class == flightClass).AvailableSeats;
}
