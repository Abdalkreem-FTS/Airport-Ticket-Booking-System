using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

public static class ManagerBookingFilterScreen
{
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
