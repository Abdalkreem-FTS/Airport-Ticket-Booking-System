using System.Globalization;
using Spectre.Console;

namespace ATBS.ConsoleUI.Prompts;

/// <summary>
/// Provides reusable Spectre.Console prompts for optional typed input.
/// </summary>
public static class PromptHelpers
{
    /// <summary>
    /// Prompts for optional text and returns null when left empty.
    /// </summary>
    public static string? OptionalText(string label)
    {
        var value = AnsiConsole.Prompt(
            new TextPrompt<string>($"[grey]{Markup.Escape(label)}[/]")
                .AllowEmpty());

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Prompts for an optional decimal value.
    /// </summary>
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

    /// <summary>
    /// Prompts for an optional date in yyyy-MM-dd format.
    /// </summary>
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

    /// <summary>
    /// Prompts for an optional GUID value.
    /// </summary>
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

    /// <summary>
    /// Waits for a key press before returning to the previous menu.
    /// </summary>
    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue.[/]");
        Console.ReadKey(intercept: true);
    }
}
