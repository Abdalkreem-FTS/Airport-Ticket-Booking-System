using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

public interface IBookingRepository
{
    Task<Result<IReadOnlyList<Booking>>> GetAllAsync();
    Task<Result<Booking>> GetByIdAsync(Guid id);
    Task<Result<IReadOnlyList<Booking>>> GetByPassengerIdAsync(Guid passengerId);
    Task<Result<Created>> AddAsync(Booking booking);
    Task<Result<Updated>> UpdateAsync(Booking booking);
}