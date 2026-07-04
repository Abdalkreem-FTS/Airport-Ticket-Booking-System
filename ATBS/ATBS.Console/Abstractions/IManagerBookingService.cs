using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Provides manager-facing booking search and filtering operations.
/// </summary>
public interface IManagerBookingService
{
    Task<Result<IReadOnlyList<Booking>>> FilterBookingsAsync(BookingSearchCriteria criteria);
}