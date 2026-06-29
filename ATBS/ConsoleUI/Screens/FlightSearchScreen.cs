using ATBS.Abstractions;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Collects flight search filters and displays matching available flights.
/// </summary>
public sealed class FlightSearchScreen(IFlightService flightService)
{
    /// <summary>
    /// Runs the passenger flight search workflow.
    /// </summary>
    public async Task RunAsync()
    {
        AppHeader.Render("Search flights");
        
        var criteria = FlightSearchPrompt.Ask();
        var flights = await flightService.SearchAvailableFlightsAsync(criteria);

        AppHeader.Render("Search flights", $"{flights.Count} result(s)");
        FlightTableRenderer.Render(flights);
        
        PromptHelpers.Pause();
    }
}
