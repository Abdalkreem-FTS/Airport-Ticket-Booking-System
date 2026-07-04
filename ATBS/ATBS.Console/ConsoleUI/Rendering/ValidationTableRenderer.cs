using ATBS.Console.DTOs;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Rendering;

/// <summary>
/// Renders model validation rules for manager guidance.
/// </summary>
public static class ValidationTableRenderer
{
    /// <summary>
    /// Displays validation metadata in a Spectre.Console table.
    /// </summary>
    public static void Render(IReadOnlyList<ValidationRuleDescription> rules)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[grey]Field[/]")
            .AddColumn("[grey]Type[/]")
            .AddColumn("[grey]Constraints[/]");

        foreach (var rule in rules)
        {
            table.AddRow(
                Markup.Escape(rule.Field),
                Markup.Escape(rule.Type),
                Markup.Escape(string.Join(", ", rule.Constraints)));
        }

        AnsiConsole.Write(table);
    }
}