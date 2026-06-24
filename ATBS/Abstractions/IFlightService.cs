using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

public interface IFlightService
{
    IReadOnlyList<Flight> SearchAvailableFlights(FlightSearchCriteria criteria);
    Flight? GetFlightById(Guid id);
}
