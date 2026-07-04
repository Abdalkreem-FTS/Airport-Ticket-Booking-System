using ATBS.Console.Models;
using FluentValidation;

namespace ATBS.Console.Validation;

/// <summary>
/// Validates flight data before it is saved or imported.
/// </summary>
public sealed class FlightValidator : AbstractValidator<Flight>
{
    public FlightValidator()
    {
        RuleFor(flight => flight.FlightNumber)
            .NotEmpty()
            .WithMessage(FlightValidationRules.RequiredMessage);

        RuleFor(flight => flight.DepartureCountry)
            .NotEmpty()
            .WithMessage(FlightValidationRules.RequiredMessage);

        RuleFor(flight => flight.DestinationCountry)
            .NotEmpty()
            .WithMessage(FlightValidationRules.RequiredMessage)
            .Must((flight, destinationCountry) => !SameText(flight.DepartureCountry, destinationCountry))
            .WithMessage(FlightValidationRules.DifferentDestinationCountryMessage);

        RuleFor(flight => flight.DepartureAirport)
            .NotEmpty()
            .WithMessage(FlightValidationRules.RequiredMessage);

        RuleFor(flight => flight.ArrivalAirport)
            .NotEmpty()
            .WithMessage(FlightValidationRules.RequiredMessage)
            .Must((flight, arrivalAirport) => !SameText(flight.DepartureAirport, arrivalAirport))
            .WithMessage(FlightValidationRules.DifferentArrivalAirportMessage);

        RuleFor(flight => flight.DepartureDate)
            .GreaterThanOrEqualTo(_ => DateTimeOffset.UtcNow.Date)
            .WithMessage(FlightValidationRules.FutureDepartureDateMessage)
            .WithState(flight => new FlightValidationState(AttemptedValue: flight.DepartureDate.ToString("O")));

        RuleFor(flight => flight.Capacity)
            .GreaterThan(0)
            .WithMessage(FlightValidationRules.PositiveCapacityMessage);

        RuleFor(flight => flight.ClassPrices)
            .NotEmpty()
            .WithMessage(FlightValidationRules.AtLeastOneClassMessage);

        RuleForEach(flight => flight.ClassPrices)
            .ChildRules(classPrice =>
            {
                classPrice.RuleFor(price => price.Price)
                    .GreaterThan(0)
                    .WithMessage(FlightValidationRules.PositiveClassPriceMessage)
                    .WithState(price => new FlightValidationState(
                        FlightValidationRules.PriceField(price.Class),
                        price.Price.ToString("F2")));

                classPrice.RuleFor(price => price.AvailableSeats)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(FlightValidationRules.NonNegativeClassSeatsMessage)
                    .WithState(price => new FlightValidationState(
                        FlightValidationRules.SeatsField(price.Class),
                        price.AvailableSeats.ToString()));
            });

        RuleFor(flight => flight.ClassPrices)
            .Must((flight, classPrices) => flight.Capacity <= 0 || classPrices.Sum(classPrice => classPrice.AvailableSeats) <= flight.Capacity)
            .WithMessage(FlightValidationRules.SeatsWithinCapacityMessage)
            .WithState(flight => new FlightValidationState(AttemptedValue: flight.ClassPrices.Sum(classPrice => classPrice.AvailableSeats).ToString()));
    }

    private static bool SameText(string left, string right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    public sealed record FlightValidationState(string? Field = null, string? AttemptedValue = null);
}