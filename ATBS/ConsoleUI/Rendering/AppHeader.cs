using Spectre.Console;

namespace ATBS.ConsoleUI.Rendering;

public static class AppHeader
{
    public static void Render(string workspace, string? context = null)
    {
        if (!Console.IsOutputRedirected)
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
