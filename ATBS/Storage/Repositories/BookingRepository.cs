using ATBS.Abstractions;
using ATBS.Models;

namespace ATBS.Storage.Repositories;

public sealed class BookingRepository(IFileStorage storage, FilePaths filePaths) : IBookingRepository
{
    public async Task<IEnumerable<Booking>> GetAllAsync() => await storage.LoadAsync<Booking>(filePaths.BookingsPath);

    public async Task<Booking?> GetByIdAsync(Guid id) => (await GetAllAsync()).FirstOrDefault(booking => booking.Id == id);

    public async Task<IEnumerable<Booking>> GetByPassengerIdAsync(Guid passengerId) => (await GetAllAsync()).Where(booking => booking.PassengerId == passengerId).ToList();

    public async Task AddAsync(Booking booking)
    {
        var bookings = (await GetAllAsync()).ToList();
        
        bookings.Add(booking);
        
        await storage.SaveAsync(filePaths.BookingsPath, bookings);
    }

    public async Task UpdateAsync(Booking booking)
    {
        var bookings = (await GetAllAsync()).ToList();
        var index = bookings.FindIndex(savedBooking => savedBooking.Id == booking.Id);
        
        if (index < 0)
        {
            throw new InvalidOperationException("Booking was not found.");
        }

        bookings[index] = booking;
        
        await storage.SaveAsync(filePaths.BookingsPath, bookings);
    }
}
