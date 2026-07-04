using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Coordinates passenger booking actions such as create, cancel, modify, and view.
/// </summary>
public interface IBookingService
{
    Task<Result<Booking>> BookFlightAsync(CreateBookingRequest request);
    Task<Result<Updated>> CancelBookingAsync(Guid passengerId, Guid bookingId);
    Task<Result<Booking>> ModifyBookingAsync(ModifyBookingRequest request);
    Task<Result<IReadOnlyList<Booking>>> GetPassengerBookingsAsync(Guid passengerId);
}