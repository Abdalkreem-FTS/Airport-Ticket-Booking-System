using ATBS.Models;

namespace ATBS.Abstractions;

public interface IFlightRepository
{
    Task<IEnumerable<Flight>> GetAllAsync();
    Task<Flight?> GetByIdAsync(Guid id);
    Task AddAsync(Flight flight);
    Task AddRangeAsync(IEnumerable<Flight> flights);
    Task UpdateAsync(Flight flight);
}
