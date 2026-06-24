using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Shows the passenger workspace and routes passenger actions to their screens.
/// </summary>
public static class PassengerMenuScreen
{
    /// <summary>
    /// Runs the passenger menu after selecting a passenger profile.
    /// </summary>
    public static void Run(AppServices services)
    {
        AppHeader.Render("Passenger workspace", "Select a passenger profile for this test session.");
        var passenger = PassengerSelectionPrompt.Ask(services.PassengerRepository.GetAll());
        if (passenger is null)
        {
            EmptyStateRenderer.Render("No passengers exist yet.");
            PromptHelpers.Pause();
            return;
        }

        while (true)
        {
            AppHeader.Render(
                "Passenger workspace",
                $"Signed in as {passenger.FirstName} {passenger.LastName} <{passenger.Email}>");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]Select action[/]")
                    .AddChoices(
                        "Search flights",
                        "Book a flight",
                        "My bookings",
                        "Modify booking",
                        "Cancel booking",
                        "Back"));

            switch (action)
            {
                case "Search flights":
                    FlightSearchScreen.Run(services);
                    break;
                case "Book a flight":
                    BookFlightScreen.Run(services, passenger);
                    break;
                case "My bookings":
                    PassengerBookingsScreen.Run(services, passenger);
                    break;
                case "Modify booking":
                    ModifyBookingScreen.Run(services, passenger);
                    break;
                case "Cancel booking":
                    CancelBookingScreen.Run(services, passenger);
                    break;
                case "Back":
                    return;
            }
        }
    }
}
