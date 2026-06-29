using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using ATBS.Models;
using ATBS.Models.Enums;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Lets a passenger cancel one of their active bookings.
/// </summary>
public static class CancelBookingScreen
{
    /// <summary>
    /// Runs the booking cancellation workflow for the selected passenger.
    /// </summary>
    public static void Run(AppServices services, Passenger passenger)
    {
        AppHeader.Render("Cancel booking", $"{passenger.FirstName} {passenger.LastName}");

        var bookings = services.BookingService.GetPassengerBookings(passenger.Id)
            .Where(booking => booking.Status == BookingStatus.Confirmed)
            .ToList();

        BookingTableRenderer.Render(bookings, services.FlightRepository);
        if (bookings.Count == 0)
        {
            PromptHelpers.Pause();
            
            return;
        }

        var booking = SelectBooking(bookings);
        var confirmed = AnsiConsole.Confirm($"Cancel booking [yellow]{booking.Id}[/]?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            services.BookingService.CancelBooking(passenger.Id, booking.Id);
            AnsiConsole.MarkupLine("[green]Booking cancelled.[/]");
        }
        catch (InvalidOperationException exception)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(exception.Message)}[/]");
        }

        PromptHelpers.Pause();
    }

    private static Booking SelectBooking(IReadOnlyList<Booking> bookings)
    {
        var choices = bookings
            .Select((booking, index) => $"{index + 1}. {booking.Id.ToString()[..8]} {booking.Class} ${booking.Price:F2}")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]Select booking[/]")
                .AddChoices(choices));

        return bookings[choices.IndexOf(selected)];
    }
}
