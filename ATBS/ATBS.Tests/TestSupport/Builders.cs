using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;

namespace ATBS.Tests.TestSupport;

/// <summary>
/// Central factory for domain objects used across tests. Every builder returns an object that is valid
/// by default; a test overrides only the field it is exercising. Keeping the defaults here means a new
/// required field breaks in one place instead of in every test.
/// </summary>
internal static class Builders
{
    public static FlightClassPrice NewClassPrice(
        FlightClass flightClass = FlightClass.Economy,
        decimal price = 100m,
        int availableSeats = 10) =>
        new() { Class = flightClass, Price = price, AvailableSeats = availableSeats };

    public static Flight NewFlight(
        Guid? id = null,
        string flightNumber = "RJ100",
        string departureCountry = "Jordan",
        string destinationCountry = "France",
        DateTimeOffset? departureDate = null,
        string departureAirport = "AMM",
        string arrivalAirport = "CDG",
        int capacity = 100,
        List<FlightClassPrice>? classPrices = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            FlightNumber = flightNumber,
            DepartureCountry = departureCountry,
            DestinationCountry = destinationCountry,
            DepartureDate = departureDate ?? DateTimeOffset.UtcNow.AddDays(7),
            DepartureAirport = departureAirport,
            ArrivalAirport = arrivalAirport,
            Capacity = capacity,
            ClassPrices = classPrices ?? [NewClassPrice()]
        };

    public static Booking NewBooking(
        Guid? id = null,
        Guid? passengerId = null,
        Guid? flightId = null,
        FlightClass flightClass = FlightClass.Economy,
        decimal price = 100m,
        BookingStatus status = BookingStatus.Confirmed,
        DateTimeOffset? bookedAt = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            PassengerId = passengerId ?? Guid.NewGuid(),
            FlightId = flightId ?? Guid.NewGuid(),
            Class = flightClass,
            Price = price,
            Status = status,
            BookedAt = bookedAt ?? DateTimeOffset.UtcNow
        };

    public static Passenger NewPassenger(Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid() };

    /// <summary>Wraps a value in a successful result.</summary>
    public static Result<T> Ok<T>(T value) => Result<T>.From(value);

    /// <summary>Wraps an error in a failed result.</summary>
    public static Result<T> Fail<T>(Error error) => error;
}
