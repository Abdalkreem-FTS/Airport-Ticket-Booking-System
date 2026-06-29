using ATBS.Models;

namespace ATBS.Abstractions;

public interface IPassengerRepository
{
    Task<IEnumerable<Passenger>> GetAllAsync();
    Task<Passenger?> GetByIdAsync(Guid id);
    Task AddAsync(Passenger passenger);
    Task UpdateAsync(Passenger passenger);
}
