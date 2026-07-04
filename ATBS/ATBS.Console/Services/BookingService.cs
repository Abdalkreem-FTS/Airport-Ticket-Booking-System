using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Services;

/// <summary>
/// Applies booking rules and keeps flight seat availability in sync. Each mutation runs in a
/// SERIALIZABLE file transaction; the factory acquires locks automatically as data is read/written
/// and retries transient conflicts (deadlock victims), so the service just expresses the logic.
/// </summary>
public sealed class BookingService(
    IBookingRepository bookingRepository,
    IFlightRepository flightRepository,
    IPassengerRepository passengerRepository,
    IFileTransactionFactory transactionFactory)
    : IBookingService
{
    public Task<Result<Booking>> BookFlightAsync(CreateBookingRequest request) =>
        transactionFactory.ExecuteAsync<Booking>(IsolationLevel.Serializable, async () =>
        {
            var passengerResult = await passengerRepository.GetByIdAsync(request.PassengerId);
            if (passengerResult.IsError)
            {
                return passengerResult.Errors;
            }

            var flightResult = await flightRepository.GetByIdAsync(request.FlightId);
            if (flightResult.IsError)
            {
                return flightResult.Errors;
            }

            var flight = flightResult.Value;
            var selectedClass = flight.ClassPrices.FirstOrDefault(price => price.Class == request.Class);
            if (selectedClass is null)
            {
                return Error.NotFound("Flights.ClassNotFound", "The requested class is not available for this flight.");
            }

            if (selectedClass.AvailableSeats <= 0)
            {
                return Error.Conflict("Flights.NoSeats", "No seats are available for the requested class.");
            }

            selectedClass.AvailableSeats--;
            var updateFlightResult = await flightRepository.UpdateAsync(flight);
            if (updateFlightResult.IsError)
            {
                return updateFlightResult.Errors;
            }

            var booking = new Booking
            {
                PassengerId = request.PassengerId,
                FlightId = request.FlightId,
                Class = request.Class,
                Price = selectedClass.Price
            };

            var addBookingResult = await bookingRepository.AddAsync(booking);
            
            return addBookingResult.IsError ? addBookingResult.Errors : booking;
        });

    public Task<Result<Updated>> CancelBookingAsync(Guid passengerId, Guid bookingId) =>
        transactionFactory.ExecuteAsync<Updated>(IsolationLevel.Serializable, async () =>
        {
            var bookingResult = await GetOwnedBookingAsync(passengerId, bookingId);
            if (bookingResult.IsError)
            {
                return bookingResult.Errors;
            }

            var booking = bookingResult.Value;
            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result.Updated;
            }

            var flightResult = await flightRepository.GetByIdAsync(booking.FlightId);
            if (flightResult.IsError)
            {
                return flightResult.Errors;
            }

            var flight = flightResult.Value;
            var selectedClass = flight.ClassPrices.FirstOrDefault(price => price.Class == booking.Class);
            if (selectedClass is null)
            {
                return Error.NotFound("Flights.BookingClassNotFound", "Booking class was not found on the flight.");
            }

            selectedClass.AvailableSeats++;
            var updateFlightResult = await flightRepository.UpdateAsync(flight);
            if (updateFlightResult.IsError)
            {
                return updateFlightResult.Errors;
            }

            booking.Status = BookingStatus.Cancelled;
            booking.LastModifiedAt = DateTimeOffset.UtcNow;
            var updateBookingResult = await bookingRepository.UpdateAsync(booking);
            
            return updateBookingResult.IsError ? updateBookingResult.Errors : Result.Updated;
        });

    public Task<Result<Booking>> ModifyBookingAsync(ModifyBookingRequest request) =>
        transactionFactory.ExecuteAsync<Booking>(IsolationLevel.Serializable, async () =>
        {
            var bookingResult = await GetOwnedBookingAsync(request.PassengerId, request.BookingId);
            if (bookingResult.IsError)
            {
                return bookingResult.Errors;
            }

            var booking = bookingResult.Value;
            if (booking.Status == BookingStatus.Cancelled)
            {
                return Error.Conflict("Bookings.Cancelled", "Cancelled bookings cannot be modified.");
            }

            var flightResult = await flightRepository.GetByIdAsync(booking.FlightId);
            if (flightResult.IsError)
            {
                return flightResult.Errors;
            }

            var flight = flightResult.Value;
            var currentClass = flight.ClassPrices.FirstOrDefault(price => price.Class == booking.Class);
            if (currentClass is null)
            {
                return Error.NotFound("Flights.BookingClassNotFound", "Booking class was not found on the flight.");
            }

            var newClass = flight.ClassPrices.FirstOrDefault(price => price.Class == request.NewClass);
            if (newClass is null)
            {
                return Error.NotFound("Flights.ClassNotFound", "The requested class is not available for this flight.");
            }

            if (newClass.Class == currentClass.Class)
            {
                return booking;
            }

            if (newClass.AvailableSeats <= 0)
            {
                return Error.Conflict("Flights.NoSeats", "No seats are available for the requested class.");
            }

            currentClass.AvailableSeats++;
            newClass.AvailableSeats--;
            var updateFlightResult = await flightRepository.UpdateAsync(flight);
            if (updateFlightResult.IsError)
            {
                return updateFlightResult.Errors;
            }

            booking.Class = newClass.Class;
            booking.Price = newClass.Price;
            booking.LastModifiedAt = DateTimeOffset.UtcNow;
            var updateBookingResult = await bookingRepository.UpdateAsync(booking);
            
            return updateBookingResult.IsError ? updateBookingResult.Errors : booking;
        });

    public async Task<Result<IReadOnlyList<Booking>>> GetPassengerBookingsAsync(Guid passengerId)
    {
        var bookingsResult = await bookingRepository.GetByPassengerIdAsync(passengerId);
        if (bookingsResult.IsError)
        {
            return bookingsResult.Errors;
        }

        return bookingsResult.Value
            .OrderByDescending(booking => booking.BookedAt)
            .ToList();
    }

    private async Task<Result<Booking>> GetOwnedBookingAsync(Guid passengerId, Guid bookingId)
    {
        var bookingResult = await bookingRepository.GetByIdAsync(bookingId);
        if (bookingResult.IsError)
        {
            return bookingResult.Errors;
        }

        var booking = bookingResult.Value;
        return booking.PassengerId != passengerId
            ? Error.Forbidden("Bookings.NotOwned", "Booking does not belong to this passenger.")
            : booking;
    }
}