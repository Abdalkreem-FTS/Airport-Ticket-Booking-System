using ATBS.Models.Enums;

namespace ATBS.DTOs;

public sealed class FlightSearchCriteria
{
    public decimal? MaxPrice { get; init; }
    public string? DepartureCountry { get; init; }
    public string? DestinationCountry { get; init; }
    public DateOnly? DepartureDate { get; init; }
    public string? DepartureAirport { get; init; }
    public string? ArrivalAirport { get; init; }
    public FlightClass? Class { get; init; }
}
