using ATBS.Console.Models.Enums;
using ATBS.Console.Validation;
using ATBS.Tests.TestSupport;

namespace ATBS.Tests.Validation;

public sealed class FlightValidatorTests
{
    private readonly FlightValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForAWellFormedFlight()
    {
        var result = _validator.Validate(Builders.NewFlight());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenFlightNumberIsEmpty()
    {
        var result = _validator.Validate(Builders.NewFlight(flightNumber: ""));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.RequiredMessage);
    }

    [Fact]
    public void Validate_Fails_WhenDestinationCountryEqualsDepartureCountry()
    {
        var result = _validator.Validate(
            Builders.NewFlight(departureCountry: "Jordan", destinationCountry: "jordan"));

        Assert.Contains(result.Errors,
            error => error.ErrorMessage == FlightValidationRules.DifferentDestinationCountryMessage);
    }

    [Fact]
    public void Validate_Fails_WhenArrivalAirportEqualsDepartureAirport()
    {
        var result = _validator.Validate(Builders.NewFlight(departureAirport: "AMM", arrivalAirport: "amm"));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.DifferentArrivalAirportMessage);
    }

    [Fact]
    public void Validate_Fails_WhenDepartureDateIsInThePast()
    {
        var result = _validator.Validate(Builders.NewFlight(departureDate: DateTimeOffset.UtcNow.AddDays(-1)));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.FutureDepartureDateMessage);
    }

    [Fact]
    public void Validate_Fails_WhenCapacityIsNotPositive()
    {
        var result = _validator.Validate(Builders.NewFlight(capacity: 0));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.PositiveCapacityMessage);
    }

    [Fact]
    public void Validate_Fails_WhenNoClassPricesProvided()
    {
        var result = _validator.Validate(Builders.NewFlight(classPrices: []));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.AtLeastOneClassMessage);
    }

    [Fact]
    public void Validate_Fails_WhenClassPriceIsNotPositive()
    {
        var result = _validator.Validate(Builders.NewFlight(classPrices: [Builders.NewClassPrice(price: 0m, availableSeats: 5)]));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.PositiveClassPriceMessage);
    }

    [Fact]
    public void Validate_Fails_WhenClassSeatsAreNegative()
    {
        var result = _validator.Validate(Builders.NewFlight(classPrices: [Builders.NewClassPrice(price: 100m, availableSeats: -1)]));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.NonNegativeClassSeatsMessage);
    }

    [Fact]
    public void Validate_Fails_WhenTotalSeatsExceedCapacity()
    {
        var result = _validator.Validate(Builders.NewFlight(capacity: 5, classPrices: [Builders.NewClassPrice()]));

        Assert.Contains(result.Errors, error => error.ErrorMessage == FlightValidationRules.SeatsWithinCapacityMessage);
    }
}
