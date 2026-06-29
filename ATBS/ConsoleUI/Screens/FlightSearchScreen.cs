using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Collects flight search filters and displays matching available flights.
/// </summary>
public static class FlightSearchScreen
{
    /// <summary>
    /// Runs the passenger flight search workflow.
    /// </summary>
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
