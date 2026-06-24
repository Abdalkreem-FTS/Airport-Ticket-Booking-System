using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

/// <summary>
/// Provides passenger-facing flight lookup operations.
/// </summary>
public interface IFlightService
{
    IReadOnlyList<Flight> SearchAvailableFlights(FlightSearchCriteria criteria);
    Flight? GetFlightById(Guid id);
}
