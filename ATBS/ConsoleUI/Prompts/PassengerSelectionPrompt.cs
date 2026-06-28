using ATBS.Models;
using Spectre.Console;

namespace ATBS.ConsoleUI.Prompts;

/// <summary>
/// Lets the tester choose which passenger profile to use.
/// </summary>
public static class PassengerSelectionPrompt
{
    /// <summary>
    /// Returns the selected passenger, or null when no passengers exist.
    /// </summary>
    public static Passenger? Ask(IReadOnlyList<Passenger> passengers)
    {
        if (passengers.Count == 0)
        {
            return null;
        }

        var choices = passengers
            .Select((passenger, index) => $"{index + 1}. {passenger.FirstName} {passenger.LastName} <{passenger.Email}>")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]Select passenger[/]")
                .PageSize(8)
                .AddChoices(choices));

        var selectedIndex = choices.IndexOf(selected);
        
        return passengers[selectedIndex];
    }
}
