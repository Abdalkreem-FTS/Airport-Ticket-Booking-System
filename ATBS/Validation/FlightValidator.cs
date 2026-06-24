using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Validation;

public sealed class FlightValidator : IValidator<Flight>
{
    public IReadOnlyList<ValidationError> Validate(Flight flight)
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
                Message = "Destination country must be different from departure country."
            });
        }

        if (SameText(flight.DepartureAirport, flight.ArrivalAirport))
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.ArrivalAirport),
                Message = "Arrival airport must be different from departure airport."
            });
        }

        if (flight.DepartureDate < DateTimeOffset.UtcNow.Date)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.DepartureDate),
                Message = "Departure date must be today or in the future.",
                AttemptedValue = flight.DepartureDate.ToString("O")
            });
        }

        if (flight.Capacity <= 0)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.Capacity),
                Message = "Capacity must be greater than zero.",
                AttemptedValue = flight.Capacity.ToString()
            });
        }

        if (flight.ClassPrices.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(flight.ClassPrices),
                Message = "At least one class price is required."
            });
        }

        foreach (var classPrice in flight.ClassPrices)
        {
            if (classPrice.Price <= 0)
            {
                errors.Add(new ValidationError
                {
                    Field = $"{nameof(flight.ClassPrices)}.{classPrice.Class}.Price",
                    Message = "Class price must be greater than zero.",
                    AttemptedValue = classPrice.Price.ToString("F2")
                });
            }

            if (classPrice.AvailableSeats < 0)
            {
                errors.Add(new ValidationError
                {
                    Field = $"{nameof(flight.ClassPrices)}.{classPrice.Class}.AvailableSeats",
                    Message = "Available seats cannot be negative.",
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
                Message = "Total class seats cannot exceed flight capacity.",
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
                Message = "Field is required."
            });
        }
    }

    private static bool SameText(string left, string right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
