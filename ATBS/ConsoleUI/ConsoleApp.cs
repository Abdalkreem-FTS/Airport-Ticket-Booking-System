using ATBS.ConsoleUI.Rendering;
using ATBS.ConsoleUI.Screens;
using Spectre.Console;

namespace ATBS.ConsoleUI;

/// <summary>
/// Runs the interactive Spectre.Console application and routes users to role workspaces.
/// </summary>
public sealed class ConsoleApp(AppServices services)
{
    /// <summary>
    /// Starts the main role selector and keeps the console app running until exit.
    /// </summary>
    public void Run()
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
                    PassengerMenuScreen.Run(services);
                    break;
                case "Manager":
                    ManagerMenuScreen.Run(services);
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[grey]Session closed.[/]");
                    return;
            }
        }
    }
}
