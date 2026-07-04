using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

public interface IFlightRepository
{
    Task<Result<IReadOnlyList<Flight>>> GetAllAsync();
    Task<Result<Flight>> GetByIdAsync(Guid id);
    Task<Result<Created>> AddAsync(Flight flight);
    Task<Result<Created>> AddRangeAsync(IEnumerable<Flight> flights);
    Task<Result<Updated>> UpdateAsync(Flight flight);
}