using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Validation;

/// <summary>
/// Validates flight data before it is saved or imported.
/// </summary>
public sealed class FlightValidator : IValidator<Flight>
{
    public IEnumerable<ValidationError> Validate(Flight flight)
    {
        var errors = new List<ValidationError>();

        Required(flight.FlightNumber, nameof(flight.FlightNumber), errors);
        Required(flight.DepartureCountry, nameof(flight.DepartureCountry), errors);
        Required(flight.DestinationCountry, nameof(flight.DestinationCountry), errors);
        Required(flight.DepartureAirport, nameof(flight.DepartureAirport), errors);
        Required(flight.ArrivalAirport, nameof(flight.ArrivalAirport), errors);

        if (SameText(flight.DepartureCountry, flight.DestinationCountry))
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.DestinationCountry),
                Message = FlightValidationRules.DifferentDestinationCountryMessage
            });
        }

        if (SameText(flight.DepartureAirport, flight.ArrivalAirport))
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.ArrivalAirport),
                Message = FlightValidationRules.DifferentArrivalAirportMessage
            });
        }

        if (flight.DepartureDate < DateTimeOffset.UtcNow.Date)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.DepartureDate),
                Message = FlightValidationRules.FutureDepartureDateMessage,
                AttemptedValue = flight.DepartureDate.ToString("O")
            });
        }

        if (flight.Capacity <= 0)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.Capacity),
                Message = FlightValidationRules.PositiveCapacityMessage,
                AttemptedValue = flight.Capacity.ToString()
            });
        }

        if (flight.ClassPrices.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.ClassPrices),
                Message = FlightValidationRules.AtLeastOneClassMessage
            });
        }

        foreach (var classPrice in flight.ClassPrices)
        {
            if (classPrice.Price <= 0)
            {
                errors.Add(new ValidationError
                {
                    Field = FlightValidationRules.PriceField(classPrice.Class),
                    Message = FlightValidationRules.PositiveClassPriceMessage,
                    AttemptedValue = classPrice.Price.ToString("F2")
                });
            }

            if (classPrice.AvailableSeats < 0)
            {
                errors.Add(new ValidationError
                {
                    Field = FlightValidationRules.SeatsField(classPrice.Class),
                    Message = FlightValidationRules.NonNegativeClassSeatsMessage,
                    AttemptedValue = classPrice.AvailableSeats.ToString()
                });
            }
        }

        var totalSeats = flight.ClassPrices.Sum(classPrice => classPrice.AvailableSeats);
        if (flight.Capacity > 0 && totalSeats > flight.Capacity)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.ClassPrices),
                Message = FlightValidationRules.SeatsWithinCapacityMessage,
                AttemptedValue = totalSeats.ToString()
            });
        }

        return errors;
    }

    private static void Required(string value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError
            {
                Field = field,
                Message = FlightValidationRules.RequiredMessage
            });
        }
    }

    private static bool SameText(string left, string right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
