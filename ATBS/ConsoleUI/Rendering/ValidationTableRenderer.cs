using ATBS.DTOs;
using Spectre.Console;

namespace ATBS.ConsoleUI.Rendering;

public static class ValidationTableRenderer
{
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
