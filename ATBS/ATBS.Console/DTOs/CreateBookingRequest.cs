using ATBS.Console.Models.Enums;

namespace ATBS.Console.DTOs;

public sealed class CreateBookingRequest
{
    public Guid PassengerId { get; init; }
    public Guid FlightId { get; init; }
    public FlightClass Class { get; init; }
}