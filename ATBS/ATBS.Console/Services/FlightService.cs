using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Extensions;
using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Services;

/// <summary>
/// Searches available flights using optional passenger-selected criteria.
/// </summary>
public sealed class FlightService(IFlightRepository flightRepository) : IFlightService
{
    public async Task<Result<IReadOnlyList<Flight>>> SearchAvailableFlightsAsync(FlightSearchCriteria criteria)
    {
        var flightsResult = await flightRepository.GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var flights = flightsResult.Value.Where(flight => flight.DepartureDate >= DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(criteria.DepartureCountry))
        {
            flights = flights.Where(flight => flight.DepartureCountry.TextEquals(criteria.DepartureCountry));
        }

        if (!string.IsNullOrWhiteSpace(criteria.DestinationCountry))
        {
            flights = flights.Where(flight => flight.DestinationCountry.TextEquals(criteria.DestinationCountry));
        }

        if (criteria.DepartureDate is not null)
        {
            flights = flights.Where(flight => DateOnly.FromDateTime(flight.DepartureDate.Date) == criteria.DepartureDate);
        }

        if (!string.IsNullOrWhiteSpace(criteria.DepartureAirport))
        {
            flights = flights.Where(flight => flight.DepartureAirport.TextEquals(criteria.DepartureAirport));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ArrivalAirport))
        {
            flights = flights.Where(flight => flight.ArrivalAirport.TextEquals(criteria.ArrivalAirport));
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

    public async Task<Result<Flight>> GetFlightByIdAsync(Guid id) => await flightRepository.GetByIdAsync(id);
}