using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

/// <summary>
/// Provides passenger-facing flight lookup operations.
/// </summary>
public interface IFlightService
{
    Task<IReadOnlyList<Flight>> SearchAvailableFlightsAsync(FlightSearchCriteria criteria);
    Task<Flight?> GetFlightByIdAsync(Guid id);
}
