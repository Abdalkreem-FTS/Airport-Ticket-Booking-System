using ATBS.Abstractions;
using ATBS.Models;

namespace ATBS.Storage.Repositories;

public sealed class BookingRepository(IFileStorage storage, FilePaths filePaths) : IBookingRepository
{
    public IReadOnlyList<Booking> GetAll() => storage.Load<Booking>(filePaths.BookingsPath);

    public Booking? GetById(Guid id) => GetAll().FirstOrDefault(booking => booking.Id == id);

    public IReadOnlyList<Booking> GetByPassengerId(Guid passengerId) => GetAll().Where(booking => booking.PassengerId == passengerId).ToList();

    public void Add(Booking booking)
    {
        var bookings = GetAll().ToList();
        
        bookings.Add(booking);
        
        storage.Save(filePaths.BookingsPath, bookings);
    }

    public void Update(Booking booking)
    {
        var bookings = GetAll().ToList();
        var index = bookings.FindIndex(savedBooking => savedBooking.Id == booking.Id);
        
        if (index < 0)
        {
            throw new InvalidOperationException("Booking was not found.");
        }

        bookings[index] = booking;
        storage.Save(filePaths.BookingsPath, bookings);
    }
}
