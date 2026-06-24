using Spectre.Console;

namespace ATBS.ConsoleUI.Rendering;

public static class EmptyStateRenderer
{
    public static void Render(string message)
    {
        AnsiConsole.Write(new Panel($"[grey]{Markup.Escape(message)}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey));
    }
}
