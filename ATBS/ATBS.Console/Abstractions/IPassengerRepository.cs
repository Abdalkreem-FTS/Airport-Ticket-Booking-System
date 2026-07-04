using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

public interface IPassengerRepository
{
    Task<Result<IReadOnlyList<Passenger>>> GetAllAsync();
    Task<Result<Passenger>> GetByIdAsync(Guid id);
    Task<Result<Created>> AddAsync(Passenger passenger);
    Task<Result<Updated>> UpdateAsync(Passenger passenger);
}