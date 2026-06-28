using ATBS.Models;

namespace ATBS.Abstractions;

public interface IPassengerRepository
{
    IReadOnlyList<Passenger> GetAll();
    Passenger? GetById(Guid id);
    void Add(Passenger passenger);
    void Update(Passenger passenger);
}
