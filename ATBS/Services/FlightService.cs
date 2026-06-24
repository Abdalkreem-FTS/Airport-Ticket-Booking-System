using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Services;

public sealed class FlightService(IFlightRepository flightRepository) : IFlightService
{
    public IReadOnlyList<Flight> SearchAvailableFlights(FlightSearchCriteria criteria)
    {
        var flights = flightRepository.GetAll().Where(flight => flight.DepartureDate >= DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(criteria.DepartureCountry))
        {
            flights = flights.Where(flight => TextEquals(flight.DepartureCountry, criteria.DepartureCountry));
        }

        if (!string.IsNullOrWhiteSpace(criteria.DestinationCountry))
        {
            flights = flights.Where(flight => TextEquals(flight.DestinationCountry, criteria.DestinationCountry));
        }

        if (criteria.DepartureDate is not null)
        {
            flights = flights.Where(flight => DateOnly.FromDateTime(flight.DepartureDate.Date) == criteria.DepartureDate);
        }

        if (!string.IsNullOrWhiteSpace(criteria.DepartureAirport))
        {
            flights = flights.Where(flight => TextEquals(flight.DepartureAirport, criteria.DepartureAirport));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ArrivalAirport))
        {
            flights = flights.Where(flight => TextEquals(flight.ArrivalAirport, criteria.ArrivalAirport));
        }

        if (criteria.Class is not null)
        {
            flights = flights.Where(flight => flight.ClassPrices.Any(price => price.Class == criteria.Class && price.AvailableSeats > 0));
        }

        if (criteria.MaxPrice is not null)
        {
            flights = flights.Where(flight => flight.ClassPrices.Any(price => price.AvailableSeats > 0 && price.Price <= criteria.MaxPrice));
        }

        return flights.OrderBy(flight => flight.DepartureDate).ToList();
    }

    public Flight? GetFlightById(Guid id) => flightRepository.GetById(id);

    private static bool TextEquals(string currentValue, string requestedValue) =>
        string.Equals(currentValue.Trim(), requestedValue.Trim(), StringComparison.OrdinalIgnoreCase);
}
