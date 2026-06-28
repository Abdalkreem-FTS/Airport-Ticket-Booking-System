using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Collects manager booking filters and displays matching bookings.
/// </summary>
public static class ManagerBookingFilterScreen
{
    /// <summary>
    /// Runs the manager booking search workflow.
    /// </summary>
    public static void Run(AppServices services)
    {
        AppHeader.Render("Filter bookings");
        
        var criteria = BookingSearchPrompt.Ask();
        var bookings = services.ManagerBookingService.FilterBookings(criteria);

        AppHeader.Render("Filter bookings", $"{bookings.Count} result(s)");
        BookingTableRenderer.Render(bookings, services.FlightRepository, services.PassengerRepository);
        
        PromptHelpers.Pause();
    }
}
