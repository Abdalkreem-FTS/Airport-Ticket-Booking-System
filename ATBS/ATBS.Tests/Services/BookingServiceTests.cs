using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Services;
using ATBS.Tests.TestSupport;
using NSubstitute;

namespace ATBS.Tests.Services;

public sealed class BookingServiceTests
{
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IFlightRepository _flights = Substitute.For<IFlightRepository>();
    private readonly IPassengerRepository _passengers = Substitute.For<IPassengerRepository>();
    private readonly IFileTransactionFactory _factory = Substitute.For<IFileTransactionFactory>();

    private BookingService CreateService() => new(_bookings, _flights, _passengers, _factory);
    
    [Fact]
    public async Task BookFlightAsync_ReservesSeat_AndCreatesBooking_OnHappyPath()
    {
        _factory.RunsWorkInline<Booking>();
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(price: 250m, availableSeats: 3)]);

        _passengers.GetByIdAsync(passengerId).Returns(Builders.Ok(Builders.NewPassenger(passengerId)));
        _flights.GetByIdAsync(flightId).Returns(Builders.Ok(flight));
        _flights.UpdateAsync(Arg.Any<Flight>()).Returns(Builders.Ok(Result.Updated));
        _bookings.AddAsync(Arg.Any<Booking>()).Returns(Builders.Ok(Result.Created));

        var request = new CreateBookingRequest
        {
            PassengerId = passengerId,
            FlightId = flightId,
            Class = FlightClass.Economy
        };

        var result = await CreateService().BookFlightAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(passengerId, result.Value.PassengerId);
        Assert.Equal(flightId, result.Value.FlightId);
        Assert.Equal(FlightClass.Economy, result.Value.Class);
        Assert.Equal(250m, result.Value.Price);
        Assert.Equal(2, flight.ClassPrices.Single().AvailableSeats);
        await _flights.Received(1).UpdateAsync(flight);
        await _bookings.Received(1).AddAsync(Arg.Is<Booking>(booking => booking.FlightId == flightId && booking.PassengerId == passengerId));
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsError_WhenPassengerNotFound()
    {
        _factory.RunsWorkInline<Booking>();
        _passengers.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Fail<Passenger>(Error.NotFound("Passengers.NotFound", "missing")));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest());

        Assert.True(result.IsError);
        Assert.Equal("Passengers.NotFound", result.TopError.Code);
        await _flights.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsError_WhenFlightNotFound()
    {
        _factory.RunsWorkInline<Booking>();
        _passengers.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(Builders.NewPassenger()));
        _flights.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Fail<Flight>(Error.NotFound("Flights.NotFound", "missing")));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest());

        Assert.True(result.IsError);
        Assert.Equal("Flights.NotFound", result.TopError.Code);
    }

    [Fact]
    public async Task BookFlightAsync_ReturnsNotFound_WhenRequestedClassIsNotOnFlight()
    {
        _factory.RunsWorkInline<Booking>();
        var flight = Builders.NewFlight(classPrices: [Builders.NewClassPrice(FlightClass.Business)]);
        _passengers.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(Builders.NewPassenger()));
        _flights.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(flight));

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
        _factory.RunsWorkInline<Booking>();
        var flight = Builders.NewFlight(classPrices: [Builders.NewClassPrice(availableSeats: 0)]);
        _passengers.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(Builders.NewPassenger()));
        _flights.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(flight));

        var result = await CreateService().BookFlightAsync(new CreateBookingRequest
        {
            Class = FlightClass.Economy
        });

        Assert.True(result.IsError);
        Assert.Equal("Flights.NoSeats", result.TopError.Code);
        Assert.Equal(ErrorType.Conflict, result.TopError.Type);
        await _bookings.DidNotReceive().AddAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task BookFlightAsync_PropagatesError_WhenBookingCannotBeSaved()
    {
        _factory.RunsWorkInline<Booking>();
        var flight = Builders.NewFlight(classPrices: [Builders.NewClassPrice(availableSeats: 1)]);
        _passengers.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(Builders.NewPassenger()));
        _flights.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Ok(flight));
        _flights.UpdateAsync(Arg.Any<Flight>()).Returns(Builders.Ok(Result.Updated));
        _bookings.AddAsync(Arg.Any<Booking>()).Returns(Builders.Fail<Created>(Error.Failure("Bookings.SaveFailed", "disk full")));

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
        _factory.RunsWorkInline<Updated>();
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId);
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(availableSeats: 5)]);

        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));
        _flights.GetByIdAsync(flightId).Returns(Builders.Ok(flight));
        _flights.UpdateAsync(Arg.Any<Flight>()).Returns(Builders.Ok(Result.Updated));
        _bookings.UpdateAsync(Arg.Any<Booking>()).Returns(Builders.Ok(Result.Updated));

        var result = await CreateService().CancelBookingAsync(passengerId, bookingId);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, flight.ClassPrices.Single().AvailableSeats);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        await _bookings.Received(1).UpdateAsync(booking);
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsForbidden_WhenBookingBelongsToAnotherPassenger()
    {
        _factory.RunsWorkInline<Updated>();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: Guid.NewGuid());
        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));

        var result = await CreateService().CancelBookingAsync(Guid.NewGuid(), bookingId);

        Assert.True(result.IsError);
        Assert.Equal("Bookings.NotOwned", result.TopError.Code);
        Assert.Equal(ErrorType.Forbidden, result.TopError.Type);
    }

    [Fact]
    public async Task CancelBookingAsync_IsIdempotent_WhenBookingAlreadyCancelled()
    {
        _factory.RunsWorkInline<Updated>();
        var passengerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, status: BookingStatus.Cancelled);
        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));

        var result = await CreateService().CancelBookingAsync(passengerId, bookingId);

        Assert.True(result.IsSuccess);
        await _flights.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        await _bookings.DidNotReceive().UpdateAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task CancelBookingAsync_PropagatesError_WhenBookingNotFound()
    {
        _factory.RunsWorkInline<Updated>();
        _bookings.GetByIdAsync(Arg.Any<Guid>()).Returns(Builders.Fail<Booking>(Error.NotFound("Bookings.NotFound", "missing")));

        var result = await CreateService().CancelBookingAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal("Bookings.NotFound", result.TopError.Code);
    }

    // ---- ModifyBookingAsync ----------------------------------------------------------------------

    [Fact]
    public async Task ModifyBookingAsync_MovesSeatsBetweenClasses_AndRepricesBooking()
    {
        _factory.RunsWorkInline<Booking>();
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Economy, price: 100m);
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(price: 100m, availableSeats: 5), Builders.NewClassPrice(FlightClass.Business, price: 300m, availableSeats: 2)]);

        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));
        _flights.GetByIdAsync(flightId).Returns(Builders.Ok(flight));
        _flights.UpdateAsync(Arg.Any<Flight>()).Returns(Builders.Ok(Result.Updated));
        _bookings.UpdateAsync(Arg.Any<Booking>()).Returns(Builders.Ok(Result.Updated));

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
        _factory.RunsWorkInline<Booking>();
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Economy);
        var flight = Builders.NewFlight(id: flightId);

        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));
        _flights.GetByIdAsync(flightId).Returns(Builders.Ok(flight));

        var result = await CreateService().ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passengerId,
            BookingId = bookingId,
            NewClass = FlightClass.Economy
        });

        Assert.True(result.IsSuccess);
        await _flights.DidNotReceive().UpdateAsync(Arg.Any<Flight>());
        await _bookings.DidNotReceive().UpdateAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task ModifyBookingAsync_ReturnsConflict_WhenBookingIsCancelled()
    {
        _factory.RunsWorkInline<Booking>();
        var passengerId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, status: BookingStatus.Cancelled);
        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));

        var result = await CreateService().ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passengerId,
            BookingId = bookingId,
            NewClass = FlightClass.Business
        });

        Assert.True(result.IsError);
        Assert.Equal("Bookings.Cancelled", result.TopError.Code);
    }

    [Fact]
    public async Task ModifyBookingAsync_ReturnsConflict_WhenTargetClassHasNoSeats()
    {
        _factory.RunsWorkInline<Booking>();
        var passengerId = Guid.NewGuid();
        var flightId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var booking = Builders.NewBooking(id: bookingId, passengerId: passengerId, flightId: flightId, flightClass: FlightClass.Economy);
        var flight = Builders.NewFlight(id: flightId, classPrices: [Builders.NewClassPrice(availableSeats: 5), Builders.NewClassPrice(FlightClass.Business, availableSeats: 0)]);

        _bookings.GetByIdAsync(bookingId).Returns(Builders.Ok(booking));
        _flights.GetByIdAsync(flightId).Returns(Builders.Ok(flight));

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
        _bookings.GetByPassengerIdAsync(passengerId).Returns(Builders.Ok<IReadOnlyList<Booking>>([older, newer]));

        var result = await CreateService().GetPassengerBookingsAsync(passengerId);

        Assert.True(result.IsSuccess);
        Assert.Equal([newer.Id, older.Id], result.Value.Select(booking => booking.Id));
    }

    [Fact]
    public async Task GetPassengerBookingsAsync_PropagatesRepositoryError()
    {
        _bookings.GetByPassengerIdAsync(Arg.Any<Guid>()).Returns(Builders.Fail<IReadOnlyList<Booking>>(Error.Failure("Bookings.LoadFailed", "io")));

        var result = await CreateService().GetPassengerBookingsAsync(Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal("Bookings.LoadFailed", result.TopError.Code);
    }
}
