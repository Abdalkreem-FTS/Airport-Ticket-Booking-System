using ATBS.Models.Enums;

namespace ATBS.Models;

/// <summary>
/// Represents a passenger reservation for a specific flight and class.
/// </summary>
public sealed class Booking
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FlightId { get; set; }
    public Guid PassengerId { get; set; }
    public FlightClass Class { get; set; }
    public decimal Price { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTimeOffset BookedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastModifiedAt { get; set; } = DateTimeOffset.UtcNow;
}
