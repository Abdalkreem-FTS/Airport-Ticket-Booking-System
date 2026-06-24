using ATBS.Models.Enums;

namespace ATBS.DTOs;

public sealed class CreateBookingRequest
{
    public Guid PassengerId { get; init; }
    public Guid FlightId { get; init; }
    public FlightClass Class { get; init; }
}
