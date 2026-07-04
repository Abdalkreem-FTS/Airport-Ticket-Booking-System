using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI.Prompts;
using ATBS.Console.ConsoleUI.Rendering;

namespace ATBS.Console.ConsoleUI.Screens;

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
        var flightsResult = await flightService.SearchAvailableFlightsAsync(criteria);
        if (flightsResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(flightsResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var flights = flightsResult.Value;

        AppHeader.Render("Search flights", $"{flights.Count} result(s)");
        FlightTableRenderer.Render(flights);
        
        PromptHelpers.Pause();
    }
}