using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Services;
using ATBS.Tests.TestSupport;
using Moq;

namespace ATBS.Tests.Services;

public sealed class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IFlightRepository> _flights = new();
    private readonly Mock<IPassengerRepository> _passengers = new();
    private readonly Mock<IFileTransactionFactory> _factory = new();

    public BookingServiceTests()
    {
        // The transaction factory just runs the unit of work inline, and persistence succeeds by default.
        // A test that needs a write to fail overrides that one repository call (Moq uses the latest setup).
        _factory.RunsWorkInline<Booking>().RunsWorkInline<Updated>();
        _flights.Setup(f => f.UpdateAsync(It.IsAny<Flight>())).ReturnsAsync(Builders.Ok(Result.Updated));
        _bookings.Setup(b => b.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(Builders.Ok(Result.Updated));
        _bookings.Setup(b => b.AddAsync(It.IsAny<Booking>())).ReturnsAsync(Builders.Ok(Result.Created));
    }

    private BookingService CreateService() => new(_bookings.Object, _flights.Object, _passengers.Object, _factory.Object);

    private void GivenPassengerExists() =>
        _passengers.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Ok(Builders.NewPassenger()));

    private void GivenFlight(Flight flight) =>
        _flights.Setup(f => f.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Ok(flight));

    private void GivenBooking(Booking booking) =>
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Ok(booking));

    [Fact]
    public async Task BookFlightAsync_ReservesSeat_AndCreatesBooking_OnHappyPath()
    {
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(price: 250m, availableSeats: 3)]);
        GivenPassengerExists();
        GivenFlight(flight);

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest
        {
            PassengerId = passengerId,
            FlightId = flightId,
            Class = FlightClass.Economy
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(passengerId, result.Value.PassengerId);
        Assert.Equal(flightId, result.Value.FlightId);
        Assert.Equal(FlightClass.Economy, result.Value.Class);
        Assert.Equal(250m, result.Value.Price);
        Assert.Equal(2, flight.ClassPrices.Single().AvailableSeats);
        _flights.Verify(f => f.UpdateAsync(flight), Times.Once);
        _bookings.Verify(b => b.AddAsync(It.Is<Booking>(booking => booking.FlightId == flightId && booking.PassengerId == passengerId)), Times.Once);
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsError_WhenPassengerNotFound()
    {
        _passengers.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Fail<Passenger>(Error.NotFound("Passengers.NotFound", "missing")));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest());

        Assert.True(result.IsError);
        Assert.Equal("Passengers.NotFound", result.TopError.Code);
        _flights.Verify(f => f.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsError_WhenFlightNotFound()
    {
        GivenPassengerExists();
        _flights.Setup(f => f.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Fail<Flight>(Error.NotFound("Flights.NotFound", "missing")));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest());

        Assert.True(result.IsError);
        Assert.Equal("Flights.NotFound", result.TopError.Code);
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsNotFound_WhenRequestedClassIsNotOnFlight()
    {
        GivenPassengerExists();
        GivenFlight(Builders.NewFlight(classPrices: [Builders.NewClassPrice(FlightClass.Business)]));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest
        {
            Class = FlightClass.Economy
        });

        Assert.True(result.IsError);
        Assert.Equal("Flights.ClassNotFound", result.TopError.Code);
        Assert.Equal(ErrorType.NotFound, result.TopError.Type);
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsConflict_WhenNoSeatsAvailable()
    {
        GivenPassengerExists();
        GivenFlight(Builders.NewFlight(classPrices: [Builders.NewClassPrice(availableSeats: 0)]));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest
        {
            Class = FlightClass.Economy
        });

        Assert.True(result.IsError);
        Assert.Equal("Flights.NoSeats", result.TopError.Code);
        Assert.Equal(ErrorType.Conflict, result.TopError.Type);
        _bookings.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task BookFlightAsync_PropagatesError_WhenBookingCannotBeSaved()
    {
        GivenPassengerExists();
        GivenFlight(Builders.NewFlight(classPrices: [Builders.NewClassPrice(availableSeats: 1)]));
        _bookings.Setup(b => b.AddAsync(It.IsAny<Booking>())).ReturnsAsync(Builders.Fail<Created>(Error.Failure("Bookings.SaveFailed", "disk full")));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest
        {
            Class = FlightClass.Economy
        });

        Assert.True(result.IsError);
        Assert.Equal("Bookings.SaveFailed", result.TopError.Code);
    }

    [Fact]
    public async Task CancelBookingAsync_ReleasesSeat_AndMarksCancelled_OnHappyPath()
    {
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId);
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(availableSeats: 5)]);
        GivenBooking(booking);
        GivenFlight(flight);

        var result = await CreateService().CancelBookingAsync(passengerId, bookingId);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, flight.ClassPrices.Single().AvailableSeats);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        _bookings.Verify(b => b.UpdateAsync(booking), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsForbidden_WhenBookingBelongsToAnotherPassenger()
    {
        GivenBooking(Builders.NewBooking(passengerId: Guid.NewGuid()));

        var result = await CreateService().CancelBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal("Bookings.NotOwned", result.TopError.Code);
        Assert.Equal(ErrorType.Forbidden, result.TopError.Type);
    }

    [Fact]
    public async Task CancelBookingAsync_IsIdempotent_WhenBookingAlreadyCancelled()
    {
        var passengerId = Guid.NewGuid();
        GivenBooking(Builders.NewBooking(passengerId: passengerId, status: BookingStatus.Cancelled));

        var result = await CreateService().CancelBookingAsync(passengerId, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        _flights.Verify(f => f.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _bookings.Verify(b => b.UpdateAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_PropagatesError_WhenBookingNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Fail<Booking>(Error.NotFound("Bookings.NotFound", "missing")));

        var result = await CreateService().CancelBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal("Bookings.NotFound", result.TopError.Code);
    }

    // ---- ModifyBookingAsync ----------------------------------------------------------------------

    [Fact]
    public async Task ModifyBookingAsync_MovesSeatsBetweenClasses_AndRepricesBooking()
    {
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Economy, price: 100m);
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(price: 100m, availableSeats: 5), Builders.NewClassPrice(FlightClass.Business, price: 300m, availableSeats: 2)]);
        GivenBooking(booking);
        GivenFlight(flight);

        var result = await CreateService().ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passengerId,
            BookingId = bookingId,
            NewClass = FlightClass.Business
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(FlightClass.Business, result.Value.Class);
        Assert.Equal(300m, result.Value.Price);
        Assert.Equal(6, flight.ClassPrices.Single(price => price.Class == FlightClass.Economy).AvailableSeats);
        Assert.Equal(1, flight.ClassPrices.Single(price => price.Class == FlightClass.Business).AvailableSeats);
    }

    [Fact]
    public async Task ModifyBookingAsync_IsNoOp_WhenNewClassEqualsCurrentClass()
    {
        var passengerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        GivenBooking(Builders.NewBooking(id: bookingId, passengerId: passengerId, flightClass: FlightClass.Economy));
        GivenFlight(Builders.NewFlight());

        var result = await CreateService().ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passengerId,
            BookingId = bookingId,
            NewClass = FlightClass.Economy
        });

        Assert.True(result.IsSuccess);
        _flights.Verify(f => f.UpdateAsync(It.IsAny<Flight>()), Times.Never);
        _bookings.Verify(b => b.UpdateAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task ModifyBookingAsync_ReturnsConflict_WhenBookingIsCancelled()
    {
        var passengerId = Guid.NewGuid();
        GivenBooking(Builders.NewBooking(passengerId: passengerId, status: BookingStatus.Cancelled));

        var result = await CreateService().ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passengerId,
            BookingId = Guid.NewGuid(),
            NewClass = FlightClass.Business
        });

        Assert.True(result.IsError);
        Assert.Equal("Bookings.Cancelled", result.TopError.Code);
    }

    [Fact]
    public async Task ModifyBookingAsync_ReturnsConflict_WhenTargetClassHasNoSeats()
    {
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        GivenBooking(Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Economy));
        GivenFlight(Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(availableSeats: 5), Builders.NewClassPrice(FlightClass.Business, availableSeats: 0)]));

        var result = await CreateService().ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passengerId,
            BookingId = bookingId,
            NewClass = FlightClass.Business
        });

        Assert.True(result.IsError);
        Assert.Equal("Flights.NoSeats", result.TopError.Code);
    }

    [Fact]
    public async Task GetPassengerBookingsAsync_ReturnsBookings_NewestFirst()
    {
        var passengerId = Guid.NewGuid();
        var older = Builders.NewBooking(passengerId: passengerId, bookedAt: DateTimeOffset.UtcNow.AddDays(-2));
        var newer = Builders.NewBooking(passengerId: passengerId, bookedAt: DateTimeOffset.UtcNow.AddHours(-1));
        _bookings.Setup(b => b.GetByPassengerIdAsync(passengerId)).ReturnsAsync(Builders.Ok<IReadOnlyList<Booking>>([older, newer]));

        var result = await CreateService().GetPassengerBookingsAsync(passengerId);

        Assert.True(result.IsSuccess);
        Assert.Equal([newer.Id, older.Id], result.Value.Select(booking => booking.Id));
    }

    [Fact]
    public async Task GetPassengerBookingsAsync_PropagatesRepositoryError()
    {
        _bookings.Setup(b => b.GetByPassengerIdAsync(It.IsAny<Guid>())).ReturnsAsync(Builders.Fail<IReadOnlyList<Booking>>(Error.Failure("Bookings.LoadFailed", "io")));

        var result = await CreateService().GetPassengerBookingsAsync(Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal("Bookings.LoadFailed", result.TopError.Code);
    }
}
