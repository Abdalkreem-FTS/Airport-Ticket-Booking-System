using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI.Prompts;
using ATBS.Console.ConsoleUI.Rendering;
using ATBS.Console.Models;

namespace ATBS.Console.ConsoleUI.Screens;

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
        
        var bookingsResult = await bookingService.GetPassengerBookingsAsync(passenger.Id);
        if (bookingsResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(bookingsResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var bookings = bookingsResult.Value;
        await BookingTableRenderer.RenderAsync(bookings, flightRepository);
        
        PromptHelpers.Pause();
    }
}