using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI.Prompts;
using ATBS.Console.ConsoleUI.Rendering;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Screens;

/// <summary>
/// Guides a passenger through searching, selecting, and confirming a flight booking.
/// </summary>
public sealed class BookFlightScreen(IFlightService flightService, IBookingService bookingService)
{
    /// <summary>
    /// Runs the complete passenger booking workflow.
    /// </summary>
    public async Task RunAsync(Passenger passenger)
    {
        AppHeader.Render("Book a flight", "Search first, then select a flight and class.");
        var criteria = FlightSearchPrompt.Ask();
        var flightsResult = await flightService.SearchAvailableFlightsAsync(criteria);
        if (flightsResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(flightsResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var flights = flightsResult.Value;

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
        var confirmed = await AnsiConsole.ConfirmAsync(
            $"Confirm booking [cyan]{Markup.Escape(flight.FlightNumber)}[/] in [cyan]{selectedClass.Value}[/] for [green]${classPrice.Price:F2}[/]?");

        if (!confirmed)
        {
            return;
        }

        var bookingResult = await bookingService.BookFlightAsync(new CreateBookingRequest
        {
            PassengerId = passenger.Id,
            FlightId = flight.Id,
            Class = selectedClass.Value
        });

        if (bookingResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(bookingResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var booking = bookingResult.Value;
        AnsiConsole.Write(new Panel(
                $"[green]Booking confirmed[/]\nID: {booking.Id}\nClass: {booking.Class}\nPrice: ${booking.Price:F2}")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));

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