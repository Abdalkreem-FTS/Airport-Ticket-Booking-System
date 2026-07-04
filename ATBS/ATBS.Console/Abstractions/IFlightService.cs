using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Provides passenger-facing flight lookup operations.
/// </summary>
public interface IFlightService
{
    Task<Result<IReadOnlyList<Flight>>> SearchAvailableFlightsAsync(FlightSearchCriteria criteria);
    Task<Result<Flight>> GetFlightByIdAsync(Guid id);
}