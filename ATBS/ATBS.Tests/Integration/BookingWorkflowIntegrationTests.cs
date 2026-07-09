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

        result.IsSuccess.Should().BeTrue();
        (await harness.ReloadFlightAsync(flight)).ClassPrices.Single().AvailableSeats.Should().Be(2);

        var booking = (await harness.ReloadBookingsAsync()).Should().ContainSingle().Which;
        booking.PassengerId.Should().Be(passenger.Id);
        booking.FlightId.Should().Be(flight.Id);
        booking.Price.Should().Be(250m);
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task BookFlight_CommitsCleanly_LeavingNoPendingTransactionLogs()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 5));

        (await harness.BookAsync(passenger, flight)).IsSuccess.Should().BeTrue();

        // A committed transaction cleans up its write-ahead log, so nothing is left for crash recovery to replay.
        harness.PendingTransactionLogFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task BookThenCancel_ReleasesSeat_AndMarksBookingCancelled()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewEconomyFlight(availableSeats: 4));

        var booked = await harness.BookAsync(passenger, flight);
        booked.IsSuccess.Should().BeTrue();

        var cancelled = await harness.BookingService.CancelBookingAsync(passenger.Id, booked.Value.Id);

        cancelled.IsSuccess.Should().BeTrue();
        (await harness.ReloadFlightAsync(flight)).ClassPrices.Single().AvailableSeats.Should().Be(4);
        (await harness.ReloadBookingsAsync()).Should().ContainSingle().Which.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task ModifyBooking_MovesSeatBetweenClasses_AndRepricesOnDisk()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewFlight(classPrices:
        [
            Builders.NewClassPrice(price: 100m, availableSeats: 5),
            Builders.NewClassPrice(FlightClass.Business, price: 300m, availableSeats: 2)
        ]));

        var booked = await harness.BookAsync(passenger, flight);
        booked.IsSuccess.Should().BeTrue();

        var modified = await harness.BookingService.ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passenger.Id,
            BookingId = booked.Value.Id,
            NewClass = FlightClass.Business
        });

        modified.IsSuccess.Should().BeTrue();
        modified.Value.Class.Should().Be(FlightClass.Business);
        modified.Value.Price.Should().Be(300m);

        var persistedFlight = await harness.ReloadFlightAsync(flight);
        // Economy: 5 - 1 (booking) + 1 (moved out) = 5. Business: 2 - 1 (moved in) = 1.
        Seats(persistedFlight, FlightClass.Economy).Should().Be(5);
        Seats(persistedFlight, FlightClass.Business).Should().Be(1);

        var booking = (await harness.ReloadBookingsAsync()).Should().ContainSingle().Which;
        booking.Class.Should().Be(FlightClass.Business);
        booking.Price.Should().Be(300m);
    }

    [Fact]
    public async Task ManagerFilter_SeesBookingsCreatedThroughTheBookingService()
    {
        await using var harness = new IntegrationTestHarness();
        var passenger = await harness.SeedPassengerAsync();
        var flight = await harness.SeedFlightAsync(Builders.NewFlight(departureCountry: "Jordan",
            classPrices: [Builders.NewClassPrice(price: 250m, availableSeats: 3)]));

        await harness.BookAsync(passenger, flight);

        // A different service reads the same persisted files and joins bookings to their flight.
        var filtered = await harness.ManagerBookingService.FilterBookingsAsync(new BookingSearchCriteria
        {
            DepartureCountry = "Jordan"
        });

        filtered.IsSuccess.Should().BeTrue();
        filtered.Value.Should().ContainSingle().Which.FlightId.Should().Be(flight.Id);
    }

    private static int Seats(Flight flight, FlightClass flightClass) =>
        flight.ClassPrices.Single(price => price.Class == flightClass).AvailableSeats;
}
