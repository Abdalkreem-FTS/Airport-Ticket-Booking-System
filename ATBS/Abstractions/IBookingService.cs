using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

public interface IBookingService
{
    Booking BookFlight(CreateBookingRequest request);
    void CancelBooking(Guid passengerId, Guid bookingId);
    Booking ModifyBooking(ModifyBookingRequest request);
    IReadOnlyList<Booking> GetPassengerBookings(Guid passengerId);
}
