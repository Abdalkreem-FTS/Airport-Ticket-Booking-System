using ATBS.Console.Abstractions;
using ATBS.Console.Models;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Storage.Repositories;

public sealed class BookingRepository(ITransactionalFileStorage storage) : IBookingRepository
{
    public async Task<Result<IReadOnlyList<Booking>>> GetAllAsync() => await storage.LoadAsync<Booking>(TransactionFile.Bookings);

    public async Task<Result<Booking>> GetByIdAsync(Guid id)
    {
        var bookingsResult = await GetAllAsync();
        if (bookingsResult.IsError)
        {
            return bookingsResult.Errors;
        }

        var booking = bookingsResult.Value.FirstOrDefault(booking => booking.Id == id);
        return booking is null
            ? Error.NotFound("Bookings.NotFound", "Booking was not found.")
            : booking;
    }

    public async Task<Result<IReadOnlyList<Booking>>> GetByPassengerIdAsync(Guid passengerId)
    {
        var bookingsResult = await GetAllAsync();
        if (bookingsResult.IsError)
        {
            return bookingsResult.Errors;
        }

        return bookingsResult.Value.Where(booking => booking.PassengerId == passengerId).ToList();
    }

    public async Task<Result<Created>> AddAsync(Booking booking)
    {
        var bookingsResult = await GetAllAsync();
        if (bookingsResult.IsError)
        {
            return bookingsResult.Errors;
        }

        var bookings = bookingsResult.Value.ToList();
        
        bookings.Add(booking);
        
        var saveResult = await storage.SaveAsync(TransactionFile.Bookings, bookings);
        return saveResult.IsError ? saveResult.Errors : Result.Created;
    }

    public async Task<Result<Updated>> UpdateAsync(Booking booking)
    {
        var bookingsResult = await GetAllAsync();
        if (bookingsResult.IsError)
        {
            return bookingsResult.Errors;
        }

        var bookings = bookingsResult.Value.ToList();
        var index = bookings.FindIndex(savedBooking => savedBooking.Id == booking.Id);
        
        if (index < 0)
        {
            return Error.NotFound("Bookings.NotFound", "Booking was not found.");
        }

        bookings[index] = booking;
        
        var saveResult = await storage.SaveAsync(TransactionFile.Bookings, bookings);
        return saveResult.IsError ? saveResult.Errors : Result.Updated;
    }
}