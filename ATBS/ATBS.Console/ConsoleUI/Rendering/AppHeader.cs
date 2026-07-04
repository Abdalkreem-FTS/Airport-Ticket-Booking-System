using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Rendering;

/// <summary>
/// Renders the shared page header and current workspace context.
/// </summary>
public static class AppHeader
{
    /// <summary>
    /// Clears the screen when possible and prints the workspace header.
    /// </summary>
    public static void Render(string workspace, string? context = null)
    {
        if (!System.Console.IsOutputRedirected)
        {
            AnsiConsole.Clear();
        }

        AnsiConsole.Write(new Rule("[bold cyan]Airport Ticket Booking System[/]").LeftJustified());

        var text = string.IsNullOrWhiteSpace(context)
            ? $"[bold]{Markup.Escape(workspace)}[/]"
            : $"[bold]{Markup.Escape(workspace)}[/]\n[grey]{Markup.Escape(context)}[/]";

        AnsiConsole.Write(new Panel(text)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Padding(1, 0));
        
        AnsiConsole.WriteLine();
    }
}