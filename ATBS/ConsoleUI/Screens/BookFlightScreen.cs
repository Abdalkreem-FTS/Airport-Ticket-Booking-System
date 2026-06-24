using ATBS.Composition;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using ATBS.DTOs;
using ATBS.Models;
using ATBS.Models.Enums;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Guides a passenger through searching, selecting, and confirming a flight booking.
/// </summary>
public static class BookFlightScreen
{
    /// <summary>
    /// Runs the complete passenger booking workflow.
    /// </summary>
    public static void Run(AppServices services, Passenger passenger)
    {
        AppHeader.Render("Book a flight", "Search first, then select a flight and class.");
        var criteria = FlightSearchPrompt.Ask();
        var flights = services.FlightService.SearchAvailableFlights(criteria);

        AppHeader.Render("Book a flight", $"{flights.Count} available flight(s)");
        FlightTableRenderer.Render(flights);

        if (flights.Count == 0)
        {
            PromptHelpers.Pause();
            return;
        }

        var flight = SelectFlight(flights);
        var selectedClass = SelectClass(flight);
        if (selectedClass is null)
        {
            EmptyStateRenderer.Render("No seats are available for this flight.");
            PromptHelpers.Pause();
            return;
        }

        var classPrice = flight.ClassPrices.First(price => price.Class == selectedClass.Value);
        var confirmed = AnsiConsole.Confirm(
            $"Confirm booking [cyan]{Markup.Escape(flight.FlightNumber)}[/] in [cyan]{selectedClass.Value}[/] for [green]${classPrice.Price:F2}[/]?");

        if (!confirmed)
        {
            return;
        }

        try
        {
            var booking = services.BookingService.BookFlight(new CreateBookingRequest
            {
                PassengerId = passenger.Id,
                FlightId = flight.Id,
                Class = selectedClass.Value
            });

            AnsiConsole.Write(new Panel(
                    $"[green]Booking confirmed[/]\nID: {booking.Id}\nClass: {booking.Class}\nPrice: ${booking.Price:F2}")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green));
        }
        catch (InvalidOperationException exception)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
        }

        PromptHelpers.Pause();
    }

    private static Flight SelectFlight(IReadOnlyList<Flight> flights)
    {
        var choices = flights
            .Select((flight, index) => $"{index + 1}. {flight.FlightNumber} {flight.DepartureAirport}->{flight.ArrivalAirport}")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]Select flight[/]")
                .PageSize(8)
                .AddChoices(choices));

        return flights[choices.IndexOf(selected)];
    }

    private static FlightClass? SelectClass(Flight flight)
    {
        var availableClasses = flight.ClassPrices
            .Where(price => price.AvailableSeats > 0)
            .ToList();

        if (availableClasses.Count == 0)
        {
            return null;
        }

        var choices = availableClasses
            .Select(price => $"{price.Class} - ${price.Price:F2} - {price.AvailableSeats} seats")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]Select class[/]")
                .AddChoices(choices));

        return availableClasses[choices.IndexOf(selected)].Class;
    }
}
