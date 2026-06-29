using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using ATBS.Abstractions;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Shows the passenger workspace and routes passenger actions to their screens.
/// </summary>
public sealed class PassengerMenuScreen(
    IPassengerRepository passengerRepository,
    FlightSearchScreen flightSearchScreen,
    BookFlightScreen bookFlightScreen,
    PassengerBookingsScreen passengerBookingsScreen,
    ModifyBookingScreen modifyBookingScreen,
    CancelBookingScreen cancelBookingScreen)
{
    /// <summary>
    /// Runs the passenger menu after selecting a passenger profile.
    /// </summary>
    public void Run()
    {
        AppHeader.Render("Passenger workspace", "Select a passenger profile for this test session.");
        var passenger = PassengerSelectionPrompt.Ask(passengerRepository.GetAll());
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
                    flightSearchScreen.Run();
                    break;
                case "Book a flight":
                    bookFlightScreen.Run(passenger);
                    break;
                case "My bookings":
                    passengerBookingsScreen.Run(passenger);
                    break;
                case "Modify booking":
                    modifyBookingScreen.Run(passenger);
                    break;
                case "Cancel booking":
                    cancelBookingScreen.Run(passenger);
                    break;
                case "Back":
                    return;
            }
        }
    }
}
