using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

/// <summary>
/// Coordinates passenger booking actions such as create, cancel, modify, and view.
/// </summary>
public interface IBookingService
{
    Booking BookFlight(CreateBookingRequest request);
    void CancelBooking(Guid passengerId, Guid bookingId);
    Booking ModifyBooking(ModifyBookingRequest request);
    IReadOnlyList<Booking> GetPassengerBookings(Guid passengerId);
}
