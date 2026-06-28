using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using ATBS.Models;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Displays the bookings that belong to the selected passenger.
/// </summary>
public static class PassengerBookingsScreen
{
    /// <summary>
    /// Shows the passenger booking history.
    /// </summary>
    public static void Run(AppServices services, Passenger passenger)
    {
        AppHeader.Render("My bookings", $"{passenger.FirstName} {passenger.LastName}");
        
        var bookings = services.BookingService.GetPassengerBookings(passenger.Id);
        BookingTableRenderer.Render(bookings, services.FlightRepository);
        
        PromptHelpers.Pause();
    }
}
