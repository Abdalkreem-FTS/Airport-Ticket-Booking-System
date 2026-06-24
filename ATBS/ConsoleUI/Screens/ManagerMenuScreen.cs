using ATBS.Composition;
using ATBS.ConsoleUI.Rendering;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

public static class ManagerMenuScreen
{
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
