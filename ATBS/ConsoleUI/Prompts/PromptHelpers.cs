using System.Globalization;
using Spectre.Console;

namespace ATBS.ConsoleUI.Prompts;

public static class PromptHelpers
{
    public static string? OptionalText(string label)
    {
        var value = AnsiConsole.Prompt(
            new TextPrompt<string>($"[grey]{Markup.Escape(label)}[/]")
                .AllowEmpty());

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static decimal? OptionalDecimal(string label)
    {
        while (true)
        {
            var value = OptionalText(label);
            if (value is null)
            {
                return null;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            AnsiConsole.MarkupLine("[red]Enter a valid number, or leave it empty.[/]");
        }
    }

    public static DateOnly? OptionalDate(string label)
    {
        while (true)
        {
            var value = OptionalText($"{label} [grey](yyyy-mm-dd)[/]");
            if (value is null)
            {
                return null;
            }

            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            AnsiConsole.MarkupLine("[red]Enter a valid date as yyyy-mm-dd, or leave it empty.[/]");
        }
    }

    public static Guid? OptionalGuid(string label)
    {
        while (true)
        {
            var value = OptionalText(label);
            if (value is null)
            {
                return null;
            }

            if (Guid.TryParse(value, out var id))
            {
                return id;
            }

            AnsiConsole.MarkupLine("[red]Enter a valid GUID, or leave it empty.[/]");
        }
    }

    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue.[/]");
        Console.ReadKey(intercept: true);
    }
}
