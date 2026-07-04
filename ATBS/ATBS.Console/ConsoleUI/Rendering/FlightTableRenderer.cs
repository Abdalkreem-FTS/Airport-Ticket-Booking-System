using ATBS.Console.Models;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Rendering;

/// <summary>
/// Renders flight search results with route, date, class, price, and seat details.
/// </summary>
public static class FlightTableRenderer
{
    /// <summary>
    /// Displays flights in a Spectre.Console table.
    /// </summary>
    public static void Render(IReadOnlyList<Flight> flights)
    {
        if (flights.Count == 0)
        {
            EmptyStateRenderer.Render("No flights matched the selected filters.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[grey]#[/]")
            .AddColumn("[grey]Flight[/]")
            .AddColumn("[grey]Route[/]")
            .AddColumn("[grey]Departure[/]")
            .AddColumn("[grey]Classes[/]");

        for (var index = 0; index < flights.Count; index++)
        {
            var flight = flights[index];
            var classes = string.Join("\n", flight.ClassPrices.Select(price =>
                $"{price.Class}: [green]${price.Price:F2}[/] [grey]({price.AvailableSeats} seats)[/]"));

            table.AddRow(
                (index + 1).ToString(),
                Markup.Escape(flight.FlightNumber),
                $"{Markup.Escape(flight.DepartureAirport)} -> {Markup.Escape(flight.ArrivalAirport)}\n[grey]{Markup.Escape(flight.DepartureCountry)} to {Markup.Escape(flight.DestinationCountry)}[/]",
                flight.DepartureDate.LocalDateTime.ToString("yyyy-MM-dd HH:mm"),
                classes);
        }

        AnsiConsole.Write(table);
    }
}