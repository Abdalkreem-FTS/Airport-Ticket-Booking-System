using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI.Prompts;
using ATBS.Console.ConsoleUI.Rendering;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Screens;

/// <summary>
/// Lets a passenger cancel one of their active bookings.
/// </summary>
public sealed class CancelBookingScreen(IBookingService bookingService, IFlightRepository flightRepository)
{
    /// <summary>
    /// Runs the booking cancellation workflow for the selected passenger.
    /// </summary>
    public async Task RunAsync(Passenger passenger)
    {
        AppHeader.Render("Cancel booking", $"{passenger.FirstName} {passenger.LastName}");

        var bookingsResult = await bookingService.GetPassengerBookingsAsync(passenger.Id);
        if (bookingsResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(bookingsResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var bookings = bookingsResult.Value
            .Where(booking => booking.Status == BookingStatus.Confirmed)
            .ToList();

        await BookingTableRenderer.RenderAsync(bookings, flightRepository);
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

        var cancelResult = await bookingService.CancelBookingAsync(passenger.Id, booking.Id);
        if (cancelResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(cancelResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        AnsiConsole.MarkupLine("[green]Booking cancelled.[/]");
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