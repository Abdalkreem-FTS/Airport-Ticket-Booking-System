using ATBS.Models;

namespace ATBS.Abstractions;

public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<Booking?> GetByIdAsync(Guid id);
    Task<IEnumerable<Booking>> GetByPassengerIdAsync(Guid passengerId);
    Task AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
}
