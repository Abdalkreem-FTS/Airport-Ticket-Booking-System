using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Models;
using ATBS.Models.Enums;

namespace ATBS.Services;

/// <summary>
/// Applies booking rules and keeps flight seat availability in sync.
/// </summary>
public sealed class BookingService(
    IBookingRepository bookingRepository,
    IFlightRepository flightRepository,
    IPassengerRepository passengerRepository)
    : IBookingService
{
    public async Task<Booking> BookFlightAsync(CreateBookingRequest request)
    {
        _ = await passengerRepository.GetByIdAsync(request.PassengerId)
            ?? throw new InvalidOperationException("Passenger was not found.");

        var flight = await flightRepository.GetByIdAsync(request.FlightId)
            ?? throw new InvalidOperationException("Flight was not found.");

        var selectedClass = flight.ClassPrices.FirstOrDefault(price => price.Class == request.Class)
            ?? throw new InvalidOperationException("The requested class is not available for this flight.");

        if (selectedClass.AvailableSeats <= 0)
        {
            throw new InvalidOperationException("No seats are available for the requested class.");
        }

        selectedClass.AvailableSeats--;
        await flightRepository.UpdateAsync(flight);

        var booking = new Booking
        {
            PassengerId = request.PassengerId,
            FlightId = request.FlightId,
            Class = request.Class,
            Price = selectedClass.Price
        };

        await bookingRepository.AddAsync(booking);
        
        return booking;
    }

    public async Task CancelBookingAsync(Guid passengerId, Guid bookingId)
    {
        var booking = await GetOwnedBookingAsync(passengerId, bookingId);
        if (booking.Status == BookingStatus.Cancelled)
        {
            return;
        }

        var flight = await flightRepository.GetByIdAsync(booking.FlightId)
            ?? throw new InvalidOperationException("Flight was not found.");

        var selectedClass = flight.ClassPrices.FirstOrDefault(price => price.Class == booking.Class)
            ?? throw new InvalidOperationException("Booking class was not found on the flight.");

        selectedClass.AvailableSeats++;
        await flightRepository.UpdateAsync(flight);

        booking.Status = BookingStatus.Cancelled;
        booking.LastModifiedAt = DateTimeOffset.UtcNow;
        await bookingRepository.UpdateAsync(booking);
    }

    public async Task<Booking> ModifyBookingAsync(ModifyBookingRequest request)
    {
        var booking = await GetOwnedBookingAsync(request.PassengerId, request.BookingId);
        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled bookings cannot be modified.");
        }

        var flight = await flightRepository.GetByIdAsync(booking.FlightId)
            ?? throw new InvalidOperationException("Flight was not found.");

        var currentClass = flight.ClassPrices.First(price => price.Class == booking.Class);
        var newClass = flight.ClassPrices.FirstOrDefault(price => price.Class == request.NewClass)
            ?? throw new InvalidOperationException("The requested class is not available for this flight.");

        if (newClass.Class == currentClass.Class)
        {
            return booking;
        }

        if (newClass.AvailableSeats <= 0)
        {
            throw new InvalidOperationException("No seats are available for the requested class.");
        }

        currentClass.AvailableSeats++;
        newClass.AvailableSeats--;
        await flightRepository.UpdateAsync(flight);

        booking.Class = newClass.Class;
        booking.Price = newClass.Price;
        booking.LastModifiedAt = DateTimeOffset.UtcNow;
        await bookingRepository.UpdateAsync(booking);
        
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetPassengerBookingsAsync(Guid passengerId) =>
        (await bookingRepository.GetByPassengerIdAsync(passengerId))
            .OrderByDescending(booking => booking.BookedAt)
            .ToList();

    private async Task<Booking> GetOwnedBookingAsync(Guid passengerId, Guid bookingId)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Booking was not found.");

        return booking.PassengerId != passengerId ? throw new InvalidOperationException("Booking does not belong to this passenger.") : booking;
    }
}
