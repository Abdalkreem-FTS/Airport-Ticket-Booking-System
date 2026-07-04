using ATBS.Console.Abstractions;
using ATBS.Console.ConsoleUI.Prompts;
using ATBS.Console.ConsoleUI.Rendering;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Screens;

/// <summary>
/// Lets a passenger change the class of an active booking.
/// </summary>
public sealed class ModifyBookingScreen(IBookingService bookingService, IFlightRepository flightRepository)
{
    /// <summary>
    /// Runs the booking modification workflow for the selected passenger.
    /// </summary>
    public async Task RunAsync(Passenger passenger)
    {
        AppHeader.Render("Modify booking", $"{passenger.FirstName} {passenger.LastName}");

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
        var flightResult = await flightRepository.GetByIdAsync(booking.FlightId);
        if (flightResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(flightResult.Errors);
            PromptHelpers.Pause();
            
            return;
        }

        var flight = flightResult.Value;
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

        var modifiedResult = await bookingService.ModifyBookingAsync(new ModifyBookingRequest
        {
            PassengerId = passenger.Id,
            BookingId = booking.Id,
            NewClass = newClass.Value
        });

        if (modifiedResult.IsError)
        {
            ErrorTableRenderer.RenderResultErrors(modifiedResult.Errors);
            PromptHelpers.Pause();
            return;
        }

        var modified = modifiedResult.Value;
        AnsiConsole.MarkupLine($"[green]Booking updated.[/] New class: [cyan]{modified.Class}[/], price: [green]${modified.Price:F2}[/]");
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