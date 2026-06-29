using ATBS.Abstractions;
using ATBS.ConsoleUI.Prompts;
using ATBS.ConsoleUI.Rendering;
using ATBS.DTOs;
using ATBS.Models;
using ATBS.Models.Enums;
using Spectre.Console;

namespace ATBS.ConsoleUI.Screens;

/// <summary>
/// Lets a passenger change the class of an active booking.
/// </summary>
public sealed class ModifyBookingScreen(IBookingService bookingService, IFlightRepository flightRepository)
{
    /// <summary>
    /// Runs the booking modification workflow for the selected passenger.
    /// </summary>
    public void Run(Passenger passenger)
    {
        AppHeader.Render("Modify booking", $"{passenger.FirstName} {passenger.LastName}");

        var bookings = bookingService.GetPassengerBookings(passenger.Id)
            .Where(booking => booking.Status == BookingStatus.Confirmed)
            .ToList();

        BookingTableRenderer.Render(bookings, flightRepository);
        if (bookings.Count == 0)
        {
            PromptHelpers.Pause();
            
            return;
        }

        var booking = SelectBooking(bookings);
        var flight = flightRepository.GetById(booking.FlightId);
        if (flight is null)
        {
            AnsiConsole.MarkupLine("[red]The flight for this booking was not found.[/]");
            PromptHelpers.Pause();
            
            return;
        }

        var newClass = SelectNewClass(flight, booking.Class);
        if (newClass is null)
        {
            EmptyStateRenderer.Render("No other classes are available for this booking.");
            PromptHelpers.Pause();
            
            return;
        }

        if (!AnsiConsole.Confirm($"Change booking class from [cyan]{booking.Class}[/] to [cyan]{newClass.Value}[/]?"))
        {
            return;
        }

        try
        {
            var modified = bookingService.ModifyBooking(new ModifyBookingRequest
            {
                PassengerId = passenger.Id,
                BookingId = booking.Id,
                NewClass = newClass.Value
            });

            AnsiConsole.MarkupLine($"[green]Booking updated.[/] New class: [cyan]{modified.Class}[/], price: [green]${modified.Price:F2}[/]");
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

    private static FlightClass? SelectNewClass(Flight flight, FlightClass currentClass)
    {
        var availableClasses = flight.ClassPrices
            .Where(price => price.Class != currentClass && price.AvailableSeats > 0)
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
                .Title("[grey]Select new class[/]")
                .AddChoices(choices));

        return availableClasses[choices.IndexOf(selected)].Class;
    }
}
