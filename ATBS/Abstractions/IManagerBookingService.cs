using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Abstractions;

/// <summary>
/// Provides manager-facing booking search and filtering operations.
/// </summary>
public interface IManagerBookingService
{
    Task<IReadOnlyList<Booking>> FilterBookingsAsync(BookingSearchCriteria criteria);
}
