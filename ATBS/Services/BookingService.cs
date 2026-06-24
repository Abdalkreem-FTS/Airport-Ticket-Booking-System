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
    public Booking BookFlight(CreateBookingRequest request)
    {
        _ = passengerRepository.GetById(request.PassengerId)
            ?? throw new InvalidOperationException("Passenger was not found.");

        var flight = flightRepository.GetById(request.FlightId)
            ?? throw new InvalidOperationException("Flight was not found.");

        var selectedClass = flight.ClassPrices.FirstOrDefault(price => price.Class == request.Class)
            ?? throw new InvalidOperationException("The requested class is not available for this flight.");

        if (selectedClass.AvailableSeats <= 0)
        {
            throw new InvalidOperationException("No seats are available for the requested class.");
        }

        selectedClass.AvailableSeats--;
        flightRepository.Update(flight);

        var booking = new Booking
        {
            PassengerId = request.PassengerId,
            FlightId = request.FlightId,
            Class = request.Class,
            Price = selectedClass.Price
        };

        bookingRepository.Add(booking);
        
        return booking;
    }

    public void CancelBooking(Guid passengerId, Guid bookingId)
    {
        var booking = GetOwnedBooking(passengerId, bookingId);
        if (booking.Status == BookingStatus.Cancelled)
        {
            return;
        }

        var flight = flightRepository.GetById(booking.FlightId)
            ?? throw new InvalidOperationException("Flight was not found.");

        var selectedClass = flight.ClassPrices.FirstOrDefault(price => price.Class == booking.Class)
            ?? throw new InvalidOperationException("Booking class was not found on the flight.");

        selectedClass.AvailableSeats++;
        flightRepository.Update(flight);

        booking.Status = BookingStatus.Cancelled;
        booking.LastModifiedAt = DateTimeOffset.UtcNow;
        bookingRepository.Update(booking);
    }

    public Booking ModifyBooking(ModifyBookingRequest request)
    {
        var booking = GetOwnedBooking(request.PassengerId, request.BookingId);
        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled bookings cannot be modified.");
        }

        var flight = flightRepository.GetById(booking.FlightId)
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
        flightRepository.Update(flight);

        booking.Class = newClass.Class;
        booking.Price = newClass.Price;
        booking.LastModifiedAt = DateTimeOffset.UtcNow;
        bookingRepository.Update(booking);
        
        return booking;
    }

    public IReadOnlyList<Booking> GetPassengerBookings(Guid passengerId) =>
        bookingRepository.GetByPassengerId(passengerId)
            .OrderByDescending(booking => booking.BookedAt)
            .ToList();

    private Booking GetOwnedBooking(Guid passengerId, Guid bookingId)
    {
        var booking = bookingRepository.GetById(bookingId)
            ?? throw new InvalidOperationException("Booking was not found.");

        return booking.PassengerId != passengerId ? throw new InvalidOperationException("Booking does not belong to this passenger.") : booking;
    }
}
