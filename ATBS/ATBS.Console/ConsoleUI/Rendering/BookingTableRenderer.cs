using ATBS.Console.Abstractions;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Rendering;

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

        foreach (var (booking, index) in bookings.Select((booking, index) => (booking, index)))
        {
            var flightResult = await flightRepository.GetByIdAsync(booking.FlightId);
            var passengerResult = passengerRepository is null
                ? null
                : await passengerRepository.GetByIdAsync(booking.PassengerId);
            var status = booking.Status == BookingStatus.Confirmed
                ? "[green]Confirmed[/]"
                : "[grey]Cancelled[/]";

            table.AddRow(
                (index + 1).ToString(),
                Markup.Escape(booking.Id.ToString()[..8]),
                passengerResult?.IsSuccess == true
                    ? Markup.Escape($"{passengerResult.Value.FirstName} {passengerResult.Value.LastName}")
                    : Markup.Escape(booking.PassengerId.ToString()[..8]),
                flightResult.IsSuccess
                    ? $"{Markup.Escape(flightResult.Value.FlightNumber)}\n[grey]{Markup.Escape(flightResult.Value.DepartureAirport)} -> {Markup.Escape(flightResult.Value.ArrivalAirport)}[/]"
                    : Markup.Escape(booking.FlightId.ToString()[..8]),
                booking.Class.ToString(),
                $"[green]${booking.Price:F2}[/]",
                status);
        }

        AnsiConsole.Write(table);
    }
}