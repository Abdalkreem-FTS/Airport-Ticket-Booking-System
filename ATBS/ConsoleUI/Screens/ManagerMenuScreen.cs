using ATBS.Composition;
using ATBS.ConsoleUI.Rendering;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Shows the manager workspace and routes manager actions to their screens.
/// </summary>
public static class ManagerMenuScreen
{
    /// <summary>
    /// Runs the manager menu for booking filters, imports, and validation details.
    /// </summary>
    public static void Run(AppServices services)
    {
        while (true)
        {
            AppHeader.Render("Manager workspace", "Manage bookings, imports, and validation details.");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]Select action[/]")
                    .AddChoices(
                        "Filter bookings",
                        "Import flights from CSV",
                        "View flight validation rules",
                        "Back"));

            switch (action)
            {
                case "Filter bookings":
                    ManagerBookingFilterScreen.Run(services);
                    break;
                case "Import flights from CSV":
                    FlightImportScreen.Run(services);
                    break;
                case "View flight validation rules":
                    ValidationRulesScreen.Run(services);
                    break;
                case "Back":
                    return;
            }
        }
    }
}
