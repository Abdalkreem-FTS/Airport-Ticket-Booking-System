using ATBS.DTOs;
using ATBS.Models.Enums;
using Spectre.Console;

namespace ATBS.ConsoleUI.Prompts;

public static class BookingSearchPrompt
{
    public static BookingSearchCriteria Ask()
    {
        AnsiConsole.MarkupLine("[grey]Leave any filter empty to skip it.[/]");
        AnsiConsole.WriteLine();

        return new BookingSearchCriteria
        {
            FlightId = PromptHelpers.OptionalGuid("Flight ID:"),
            PassengerId = PromptHelpers.OptionalGuid("Passenger ID:"),
            MaxPrice = PromptHelpers.OptionalDecimal("Maximum price:"),
            DepartureCountry = PromptHelpers.OptionalText("Departure country:"),
            DestinationCountry = PromptHelpers.OptionalText("Destination country:"),
            DepartureDate = PromptHelpers.OptionalDate("Departure date:"),
            DepartureAirport = PromptHelpers.OptionalText("Departure airport:"),
            ArrivalAirport = PromptHelpers.OptionalText("Arrival airport:"),
            Class = AskOptionalClass()
        };
    }

    private static FlightClass? AskOptionalClass()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]Class[/]")
                .AddChoices("Any", "Economy", "Business", "First Class"));

        return choice switch
        {
            "Economy" => FlightClass.Economy,
            "Business" => FlightClass.Business,
            "First Class" => FlightClass.FirstClass,
            _ => null
        };
    }
}
