using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Services;
using ATBS.Tests.TestSupport;
using Moq;

namespace ATBS.Tests.Services;

public sealed class ManagerBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IFlightRepository> _flights = new();

    private ManagerBookingService CreateService() => new(_bookings.Object, _flights.Object);

    private void GivenBookings(params Booking[] bookings) =>
        _bookings.Setup(b => b.GetAllAsync()).ReturnsAsync(Builders.Ok<IReadOnlyList<Booking>>(bookings.ToList()));

    private void GivenFlights(params Flight[] flights) =>
        _flights.Setup(f => f.GetAllAsync()).ReturnsAsync(Builders.Ok<IReadOnlyList<Flight>>(flights.ToList()));

    [Fact]
    public async Task FilterBookingsAsync_ReturnsAll_NewestFirst_WhenNoCriteria()
    {
        var older = Builders.NewBooking(bookedAt: DateTimeOffset.UtcNow.AddDays(-3));
        var newer = Builders.NewBooking(bookedAt: DateTimeOffset.UtcNow.AddHours(-1));
        GivenBookings(older, newer);

        var result = await CreateService().FilterBookingsAsync(new BookingSearchCriteria());

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(booking => booking.Id).Should().Equal(newer.Id, older.Id);
    }

    [Fact]
    public async Task FilterBookingsAsync_DoesNotLoadFlights_WhenOnlyBookingFiltersUsed()
    {
        GivenBookings(Builders.NewBooking());

        await CreateService().FilterBookingsAsync(new BookingSearchCriteria
        {
            MaxPrice = 500m
        });

        _flights.Verify(f => f.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task FilterBookingsAsync_FiltersByFlightAndPassengerAndClassAndPrice()
    {
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var match = Builders.NewBooking(passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Business, price: 300m);
        var wrongPassenger = Builders.NewBooking(flightId: flightId, flightClass: FlightClass.Business, price: 300m);
        var wrongClass = Builders.NewBooking(passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Economy, price: 300m);
        var tooExpensive = Builders.NewBooking(passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Business, price: 900m);
        GivenBookings(match, wrongPassenger, wrongClass, tooExpensive);

        var result = await CreateService().FilterBookingsAsync(new BookingSearchCriteria
        {
            FlightId = flightId,
            PassengerId = passengerId,
            Class = FlightClass.Business,
            MaxPrice = 500m
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(match.Id);
    }

    [Fact]
    public async Task FilterBookingsAsync_AppliesFlightFilters_UsingRelatedFlight()
    {
        var jordanFlightId = Guid.NewGuid();
        var egyptFlightId = Guid.NewGuid();
        var jordanBooking = Builders.NewBooking(flightId: jordanFlightId);
        var egyptBooking = Builders.NewBooking(flightId: egyptFlightId);
        GivenBookings(jordanBooking, egyptBooking);
        GivenFlights(Builders.NewFlight(id: jordanFlightId, departureCountry: "Jordan"), Builders.NewFlight(id: egyptFlightId, departureCountry: "Egypt"));

        var result = await CreateService().FilterBookingsAsync(new BookingSearchCriteria
        {
            DepartureCountry = "Jordan"
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(jordanBooking.Id);
    }

    [Fact]
    public async Task FilterBookingsAsync_ExcludesBooking_WhenItsFlightIsMissing()
    {
        var bookingWithFlight = Builders.NewBooking(flightId: Guid.NewGuid());
        var orphanBooking = Builders.NewBooking(flightId: Guid.NewGuid());
        GivenBookings(bookingWithFlight, orphanBooking);
        GivenFlights(Builders.NewFlight(id: bookingWithFlight.FlightId, departureCountry: "Jordan"));

        var result = await CreateService().FilterBookingsAsync(new BookingSearchCriteria
        {
            DepartureCountry = "Jordan"
        });

        result.Value.Should().ContainSingle().Which.Id.Should().Be(bookingWithFlight.Id);
    }

    [Fact]
    public async Task FilterBookingsAsync_PropagatesBookingRepositoryError()
    {
        _bookings.Setup(b => b.GetAllAsync()).ReturnsAsync(Builders.Fail<IReadOnlyList<Booking>>(Error.Failure("Bookings.LoadFailed", "io")));

        var result = await CreateService().FilterBookingsAsync(new BookingSearchCriteria());

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Bookings.LoadFailed");
    }

    [Fact]
    public async Task FilterBookingsAsync_PropagatesFlightRepositoryError_WhenFlightFiltersRequested()
    {
        GivenBookings(Builders.NewBooking());
        _flights.Setup(f => f.GetAllAsync()).ReturnsAsync(Builders.Fail<IReadOnlyList<Flight>>(Error.Failure("Flights.LoadFailed", "io")));

        var result = await CreateService().FilterBookingsAsync(new BookingSearchCriteria
        {
            ArrivalAirport = "CDG"
        });

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Flights.LoadFailed");
    }
}
