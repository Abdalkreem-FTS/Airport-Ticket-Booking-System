using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

public interface IManagerBookingService
{
    IReadOnlyList<Booking> FilterBookings(BookingSearchCriteria criteria);
}
