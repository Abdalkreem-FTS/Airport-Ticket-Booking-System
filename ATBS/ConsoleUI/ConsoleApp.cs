using ATBS.Composition;
using ATBS.ConsoleUI.Rendering;
using ATBS.ConsoleUI.Screens;
using Spectre.Console;

namespace ATBS.ConsoleUI;

public sealed class ConsoleApp(AppServices services)
{
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
