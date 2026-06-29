using ATBS.Abstractions;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using ATBS.Models;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Displays the bookings that belong to the selected passenger.
/// </summary>
public sealed class PassengerBookingsScreen(IBookingService bookingService, IFlightRepository flightRepository)
{
    /// <summary>
    /// Shows the passenger booking history.
    /// </summary>
    public void Run(Passenger passenger)
    {
        AppHeader.Render("My bookings", $"{passenger.FirstName} {passenger.LastName}");
        
        var bookings = bookingService.GetPassengerBookings(passenger.Id);
        BookingTableRenderer.Render(bookings, flightRepository);
        
        PromptHelpers.Pause();
    }
}
