using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI.Prompts;
using ATBS.Console.ConsoleUI.Rendering;

namespace ATBS.Console.ConsoleUI.Screens;

/// <summary>
/// Collects manager booking filters and displays matching bookings.
/// </summary>
public sealed class ManagerBookingFilterScreen(
    IManagerBookingService managerBookingService,
    IFlightRepository flightRepository,
    IPassengerRepository passengerRepository)
{
    /// <summary>
    /// Runs the manager booking search workflow.
    /// </summary>
    public async Task RunAsync()
    {
        AppHeader.Render("Filter bookings");
        
        var criteria = BookingSearchPrompt.Ask();
        var bookingsResult = await managerBookingService.FilterBookingsAsync(criteria);
        if (bookingsResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(bookingsResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var bookings = bookingsResult.Value;

        AppHeader.Render("Filter bookings", $"{bookings.Count} result(s)");
        await BookingTableRenderer.RenderAsync(bookings, flightRepository, passengerRepository);
        
        PromptHelpers.Pause();
    }
}