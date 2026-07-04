using ATBS.Console.Models.Enums;

namespace ATBS.Console.DTOs;

public sealed class ModifyBookingRequest
{
    public Guid PassengerId { get; init; }
    public Guid BookingId { get; init; }
    public FlightClass NewClass { get; init; }
}