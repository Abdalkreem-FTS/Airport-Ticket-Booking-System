namespace ATBS.DTOs;

public sealed class ImportFlightRow
{
    public int RowNumber { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public string DepartureCountry { get; init; } = string.Empty;
    public string DestinationCountry { get; init; } = string.Empty;
    public string DepartureDate { get; init; } = string.Empty;
    public string DepartureAirport { get; init; } = string.Empty;
    public string ArrivalAirport { get; init; } = string.Empty;
    public string Capacity { get; init; } = string.Empty;
    public string EconomyPrice { get; init; } = string.Empty;
    public string EconomySeats { get; init; } = string.Empty;
    public string BusinessPrice { get; init; } = string.Empty;
    public string BusinessSeats { get; init; } = string.Empty;
    public string FirstClassPrice { get; init; } = string.Empty;
    public string FirstClassSeats { get; init; } = string.Empty;
}
