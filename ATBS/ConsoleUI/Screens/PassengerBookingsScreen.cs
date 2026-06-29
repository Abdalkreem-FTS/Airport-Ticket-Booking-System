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
    public async Task RunAsync(Passenger passenger)
    {
        AppHeader.Render("My bookings", $"{passenger.FirstName} {passenger.LastName}");
        
        var bookings = await bookingService.GetPassengerBookingsAsync(passenger.Id);
        await BookingTableRenderer.RenderAsync(bookings, flightRepository);
        
        PromptHelpers.Pause();
    }
}
