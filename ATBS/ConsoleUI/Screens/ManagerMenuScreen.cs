using ATBS.ConsoleUI.Rendering;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Shows the manager workspace and routes manager actions to their screens.
/// </summary>
public sealed class ManagerMenuScreen(
    ManagerBookingFilterScreen managerBookingFilterScreen,
    FlightImportScreen flightImportScreen,
    ValidationRulesScreen validationRulesScreen)
{
    /// <summary>
    /// Runs the manager menu for booking filters, imports, and validation details.
    /// </summary>
    public void Run()
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
                    managerBookingFilterScreen.Run();
                    break;
                case "Import flights from CSV":
                    flightImportScreen.Run();
                    break;
                case "View flight validation rules":
                    validationRulesScreen.Run();
                    break;
                case "Back":
                    return;
            }
        }
    }
}
