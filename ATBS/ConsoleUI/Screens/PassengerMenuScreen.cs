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
    public async Task RunAsync()
    {
        AppHeader.Render("Passenger workspace", "Select a passenger profile for this test session.");
        var passenger = PassengerSelectionPrompt.Ask(await passengerRepository.GetAllAsync());
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
                    await flightSearchScreen.RunAsync();
                    break;
                case "Book a flight":
                    await bookFlightScreen.RunAsync(passenger);
                    break;
                case "My bookings":
                    await passengerBookingsScreen.RunAsync(passenger);
                    break;
                case "Modify booking":
                    await modifyBookingScreen.RunAsync(passenger);
                    break;
                case "Cancel booking":
                    await cancelBookingScreen.RunAsync(passenger);
                    break;
                case "Back":
                    return;
            }
        }
    }
}
