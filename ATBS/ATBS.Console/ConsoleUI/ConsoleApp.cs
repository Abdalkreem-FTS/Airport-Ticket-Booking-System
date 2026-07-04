using ATBS.Console.ConsoleUI.Rendering;
using ATBS.Console.ConsoleUI.Screens;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI;

/// <summary>
/// Runs the interactive Spectre.Console application and routes users to role workspaces.
/// </summary>
public sealed class ConsoleApp(PassengerMenuScreen passengerMenuScreen, ManagerMenuScreen managerMenuScreen)
{
    /// <summary>
    /// Starts the main role selector and keeps the console app running until exit.
    /// </summary>
    public async Task RunAsync()
    {
        while (true)
        {
            AppHeader.Render("Main workspace", "Choose the role you want to test.");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]Open workspace[/]")
                    .AddChoices("Passenger", "Manager", "Exit"));

            switch (action)
            {
                case "Passenger":
                    await passengerMenuScreen.RunAsync();
                    break;
                case "Manager":
                    await managerMenuScreen.RunAsync();
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[grey]Session closed.[/]");
                    return;
            }
        }
    }
}