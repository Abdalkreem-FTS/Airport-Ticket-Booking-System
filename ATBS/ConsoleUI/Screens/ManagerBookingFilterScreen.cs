using ATBS.Abstractions;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

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
        var bookings = await managerBookingService.FilterBookingsAsync(criteria);

        AppHeader.Render("Filter bookings", $"{bookings.Count} result(s)");
        await BookingTableRenderer.RenderAsync(bookings, flightRepository, passengerRepository);
        
        PromptHelpers.Pause();
    }
}
