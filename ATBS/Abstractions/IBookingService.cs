using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

/// <summary>
/// Coordinates passenger booking actions such as create, cancel, modify, and view.
/// </summary>
public interface IBookingService
{
    Task<Booking> BookFlightAsync(CreateBookingRequest request);
    Task CancelBookingAsync(Guid passengerId, Guid bookingId);
    Task<Booking> ModifyBookingAsync(ModifyBookingRequest request);
    Task<IReadOnlyList<Booking>> GetPassengerBookingsAsync(Guid passengerId);
}
