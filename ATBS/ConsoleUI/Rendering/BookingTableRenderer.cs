using ATBS.Abstractions;
using ATBS.Models;
using ATBS.Models.Enums;
using Spectre.Console;

namespace ATBS.ConsoleUI.Rendering;

/// <summary>
/// Renders booking records with related passenger and flight information.
/// </summary>
public static class BookingTableRenderer
{
    /// <summary>
    /// Displays bookings in a Spectre.Console table.
    /// </summary>
    public static async Task RenderAsync(
        IReadOnlyList<Booking> bookings,
        IFlightRepository flightRepository,
        IPassengerRepository? passengerRepository = null)
    {
        if (bookings.Count == 0)
        {
            EmptyStateRenderer.Render("No bookings to show.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[grey]#[/]")
            .AddColumn("[grey]Booking[/]")
            .AddColumn("[grey]Passenger[/]")
            .AddColumn("[grey]Flight[/]")
            .AddColumn("[grey]Class[/]")
            .AddColumn("[grey]Price[/]")
            .AddColumn("[grey]Status[/]");

        for (var index = 0; index < bookings.Count; index++)
        {
            var booking = bookings[index];
            var flight = await flightRepository.GetByIdAsync(booking.FlightId);
            var passenger = passengerRepository is null
                ? null
                : await passengerRepository.GetByIdAsync(booking.PassengerId);
            var status = booking.Status == BookingStatus.Confirmed
                ? "[green]Confirmed[/]"
                : "[grey]Cancelled[/]";

            table.AddRow(
                (index + 1).ToString(),
                Markup.Escape(booking.Id.ToString()[..8]),
                passenger is null
                    ? Markup.Escape(booking.PassengerId.ToString()[..8])
                    : Markup.Escape($"{passenger.FirstName} {passenger.LastName}"),
                flight is null
                    ? Markup.Escape(booking.FlightId.ToString()[..8])
                    : $"{Markup.Escape(flight.FlightNumber)}\n[grey]{Markup.Escape(flight.DepartureAirport)} -> {Markup.Escape(flight.ArrivalAirport)}[/]",
                booking.Class.ToString(),
                $"[green]${booking.Price:F2}[/]",
                status);
        }

        AnsiConsole.Write(table);
    }
}
