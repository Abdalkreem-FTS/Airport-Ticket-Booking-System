using ATBS.Console.DTOs;
using ATBS.Console.Models.Enums;
using Spectre.Console;

namespace ATBS.Console.ConsoleUI.Prompts;

/// <summary>
/// Collects optional filters for passenger flight searches.
/// </summary>
public static class FlightSearchPrompt
{
    /// <summary>
    /// Builds flight search criteria from console input.
    /// </summary>
    public static FlightSearchCriteria Ask()
    {
        AnsiConsole.MarkupLine("[grey]Leave any filter empty to skip it.[/]");
        AnsiConsole.WriteLine();

        return new FlightSearchCriteria
        {
            DepartureCountry = PromptHelpers.OptionalText("Departure country:"),
            DestinationCountry = PromptHelpers.OptionalText("Destination country:"),
            DepartureDate = PromptHelpers.OptionalDate("Departure date:"),
            DepartureAirport = PromptHelpers.OptionalText("Departure airport:"),
            ArrivalAirport = PromptHelpers.OptionalText("Arrival airport:"),
            Class = AskOptionalClass(),
            MaxPrice = PromptHelpers.OptionalDecimal("Maximum price:")
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