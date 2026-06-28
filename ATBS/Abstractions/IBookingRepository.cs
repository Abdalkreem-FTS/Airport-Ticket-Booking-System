using ATBS.Models;

namespace ATBS.Abstractions;

public interface IBookingRepository
{
    IReadOnlyList<Booking> GetAll();
    Booking? GetById(Guid id);
    IReadOnlyList<Booking> GetByPassengerId(Guid passengerId);
    void Add(Booking booking);
    void Update(Booking booking);
}
