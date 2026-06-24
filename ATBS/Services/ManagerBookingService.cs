using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Models;

namespace ATBS.Services;

/// <summary>
/// Filters bookings for managers using booking, passenger, and flight criteria.
/// </summary>
public sealed class ManagerBookingService(IBookingRepository bookingRepository, IFlightRepository flightRepository)
    : IManagerBookingService
{
    public IReadOnlyList<Booking> FilterBookings(BookingSearchCriteria criteria)
    {
        var bookings = bookingRepository.GetAll().AsEnumerable();

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
        if (!flightFiltersRequested) return bookings.OrderByDescending(booking => booking.BookedAt).ToList();
        {
            var flightsById = flightRepository.GetAll().ToDictionary(flight => flight.Id);
            bookings = bookings.Where(booking =>
                flightsById.TryGetValue(booking.FlightId, out var flight) && MatchesFlightFilters(flight, criteria));
        }

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
            && !TextEquals(flight.DepartureCountry, criteria.DepartureCountry))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.DestinationCountry)
            && !TextEquals(flight.DestinationCountry, criteria.DestinationCountry))
        {
            return false;
        }

        if (criteria.DepartureDate is not null
            && DateOnly.FromDateTime(flight.DepartureDate.Date) != criteria.DepartureDate)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.DepartureAirport)
            && !TextEquals(flight.DepartureAirport, criteria.DepartureAirport))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(criteria.ArrivalAirport)
               || TextEquals(flight.ArrivalAirport, criteria.ArrivalAirport);
    }

    private static bool TextEquals(string currentValue, string requestedValue) =>
        string.Equals(currentValue.Trim(), requestedValue.Trim(), StringComparison.OrdinalIgnoreCase);
}
