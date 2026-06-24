using ATBS.Models.Enums;

namespace ATBS.Models;

public sealed class FlightClassPrice
{
    public FlightClass Class { get; init; }
    public decimal Price { get; init; }
    public int AvailableSeats { get; set; }
}
