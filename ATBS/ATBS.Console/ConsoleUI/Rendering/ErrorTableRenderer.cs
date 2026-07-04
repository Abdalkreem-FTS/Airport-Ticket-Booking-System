using ATBS.Console.DTOs;
using ATBS.Console.Results;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Rendering;

/// <summary>
/// Renders validation and import errors in a consistent table format.
/// </summary>
public static class ErrorTableRenderer
{
    /// <summary>
    /// Displays row-level validation errors.
    /// </summary>
    public static void Render(IReadOnlyList<ValidationError> errors)
    {
        if (errors.Count == 0)
        {
            EmptyStateRenderer.Render("No validation errors.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Red)
            .AddColumn("[grey]Row[/]")
            .AddColumn("[grey]Field[/]")
            .AddColumn("[grey]Message[/]")
            .AddColumn("[grey]Value[/]");

        foreach (var error in errors)
        {
            table.AddRow(
                error.RowNumber?.ToString() ?? "-",
                Markup.Escape(error.Field),
                Markup.Escape(error.Message),
                Markup.Escape(error.AttemptedValue ?? "-"));
        }

        AnsiConsole.Write(table);
    }

    public static void RenderResultErrors(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            EmptyStateRenderer.Render("No errors.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Red)
            .AddColumn("[grey]Type[/]")
            .AddColumn("[grey]Code[/]")
            .AddColumn("[grey]Description[/]");

        foreach (var error in errors)
        {
            table.AddRow(
                Markup.Escape(error.Type.ToString()),
                Markup.Escape(error.Code),
                Markup.Escape(error.Description));
        }

        AnsiConsole.Write(table);
    }
}