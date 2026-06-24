namespace ATBS.Models;

/// <summary>
/// Represents a scheduled flight with route details, capacity, and class pricing.
/// </summary>
public sealed class Flight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FlightNumber { get; init; } = string.Empty;
    public string DepartureCountry { get; init; } = string.Empty;
    public string DestinationCountry { get; init; } = string.Empty;
    public DateTimeOffset DepartureDate { get; init; }
    public string DepartureAirport { get; init; } = string.Empty;
    public string ArrivalAirport { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public List<FlightClassPrice> ClassPrices { get; init; } = [];
}
