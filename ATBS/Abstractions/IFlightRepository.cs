using ATBS.Models;

namespace ATBS.Abstractions;

public interface IFlightRepository
{
    IReadOnlyList<Flight> GetAll();
    Flight? GetById(Guid id);
    void Add(Flight flight);
    void AddRange(IEnumerable<Flight> flights);
    void Update(Flight flight);
    void Delete(Guid id);
}
