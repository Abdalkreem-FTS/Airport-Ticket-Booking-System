using ATBS.Models.Enums;

namespace ATBS.DTOs;

public sealed class ModifyBookingRequest
{
    public Guid PassengerId { get; init; }
    public Guid BookingId { get; init; }
    public FlightClass NewClass { get; init; }
}
