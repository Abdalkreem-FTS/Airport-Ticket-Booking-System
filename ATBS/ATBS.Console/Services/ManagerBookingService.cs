using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Extensions;
using ATBS.Console.Models;
using ATBS.Console.Results;

namespace ATBS.Console.Services;

/// <summary>
/// Filters bookings for managers using booking, passenger, and flight criteria.
/// </summary>
public sealed class ManagerBookingService(IBookingRepository bookingRepository, IFlightRepository flightRepository)
    : IManagerBookingService
{
    public async Task<Result<IReadOnlyList<Booking>>> FilterBookingsAsync(BookingSearchCriteria criteria)
    {
        var bookingsResult = await bookingRepository.GetAllAsync();
        if (bookingsResult.IsError)
        {
            return bookingsResult.Errors;
        }

        IEnumerable<Booking> bookings = bookingsResult.Value;

        if (criteria.FlightId is not null)
        {
            bookings = bookings.Where(booking => booking.FlightId == criteria.FlightId);
        }

        if (criteria.PassengerId is not null)
        {
            bookings = bookings.Where(booking => booking.PassengerId == criteria.PassengerId);
        }

        if (criteria.MaxPrice is not null)
        {
            bookings = bookings.Where(booking => booking.Price <= criteria.MaxPrice);
        }

        if (criteria.Class is not null)
        {
            bookings = bookings.Where(booking => booking.Class == criteria.Class);
        }

        var flightFiltersRequested = HasFlightFilters(criteria);
        if (!flightFiltersRequested)
        {
            return bookings.OrderByDescending(booking => booking.BookedAt).ToList();
        }


        var flightsResult = await flightRepository.GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var flightsById = flightsResult.Value.ToDictionary(flight => flight.Id);
        bookings = bookings.Where(booking => flightsById.TryGetValue(booking.FlightId, out var flight) && MatchesFlightFilters(flight, criteria));


        return bookings.OrderByDescending(booking => booking.BookedAt).ToList();
    }

    private static bool HasFlightFilters(BookingSearchCriteria criteria) =>
        !string.IsNullOrWhiteSpace(criteria.DepartureCountry)
        || !string.IsNullOrWhiteSpace(criteria.DestinationCountry)
        || criteria.DepartureDate is not null
        || !string.IsNullOrWhiteSpace(criteria.DepartureAirport)
        || !string.IsNullOrWhiteSpace(criteria.ArrivalAirport);

    private static bool MatchesFlightFilters(Flight flight, BookingSearchCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.DepartureCountry)
            && !flight.DepartureCountry.TextEquals(criteria.DepartureCountry))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.DestinationCountry)
            && !flight.DestinationCountry.TextEquals(criteria.DestinationCountry))
        {
            return false;
        }

        if (criteria.DepartureDate is not null
            && DateOnly.FromDateTime(flight.DepartureDate.Date) != criteria.DepartureDate)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.DepartureAirport)
            && !flight.DepartureAirport.TextEquals(criteria.DepartureAirport))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(criteria.ArrivalAirport)
               || flight.ArrivalAirport.TextEquals(criteria.ArrivalAirport);
    }
}