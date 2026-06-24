using ATBS.Models.Enums;

namespace ATBS.Models;

/// <summary>
/// Describes the price and available seats for one class on a flight.
/// </summary>
public sealed class FlightClassPrice
{
    public FlightClass Class { get; init; }
    public decimal Price { get; init; }
    public int AvailableSeats { get; set; }
}
