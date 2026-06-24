using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

public static class FlightSearchScreen
{
    public static void Run(AppServices services)
    {
        AppHeader.Render("Search flights");
        var criteria = FlightSearchPrompt.Ask();
        var flights = services.FlightService.SearchAvailableFlights(criteria);

        AppHeader.Render("Search flights", $"{flights.Count} result(s)");
        FlightTableRenderer.Render(flights);
        PromptHelpers.Pause();
    }
}
