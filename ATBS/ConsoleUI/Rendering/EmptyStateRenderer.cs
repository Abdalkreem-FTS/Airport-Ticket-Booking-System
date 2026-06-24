using Spectre.Console;

namespace ATBS.ConsoleUI.Rendering;

/// <summary>
/// Renders a consistent empty-state message when no data is available.
/// </summary>
public static class EmptyStateRenderer
{
    /// <summary>
    /// Displays a muted message panel for empty results.
    /// </summary>
    public static void Render(string message)
    {
        AnsiConsole.Write(new Panel($"[grey]{Markup.Escape(message)}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey));
    }
}
