using ATBS.DTOs;
using ATBS.Models.Enums;

namespace ATBS.Validation;

/// <summary>
/// Shared flight validation field names, messages, and import guidance.
/// </summary>
public static class FlightValidationRules
{
    public const string RequiredMessage = "Field is required.";
    public const string DifferentDestinationCountryMessage = "Destination country must be different from departure country.";
    public const string DifferentArrivalAirportMessage = "Arrival airport must be different from departure airport.";
    public const string FutureDepartureDateMessage = "Departure date must be today or in the future.";
    public const string PositiveCapacityMessage = "Capacity must be greater than zero.";
    public const string AtLeastOneClassMessage = "At least one class price is required.";
    public const string PositiveClassPriceMessage = "Class price must be greater than zero.";
    public const string NonNegativeClassSeatsMessage = "Available seats cannot be negative.";
    public const string SeatsWithinCapacityMessage = "Total class seats cannot exceed flight capacity.";

    public static IReadOnlyList<ValidationRuleDescription> Descriptions { get; } =
    [
        Rule("FlightNumber", "Free Text", RequiredMessage),
        Rule("DepartureCountry", "Free Text", RequiredMessage),
        Rule("DestinationCountry", "Free Text", RequiredMessage, DifferentDestinationCountryMessage),
        Rule("DepartureDate", "Date Time", RequiredMessage, FutureDepartureDateMessage),
        Rule("DepartureAirport", "Free Text", RequiredMessage),
        Rule("ArrivalAirport", "Free Text", RequiredMessage, DifferentArrivalAirportMessage),
        Rule("Capacity", "Number", RequiredMessage, PositiveCapacityMessage),
        Rule("ClassPrices", "Grouped Values", AtLeastOneClassMessage, SeatsWithinCapacityMessage),
        Rule("EconomyPrice", "Money", "Required when economy seats are provided.", PositiveClassPriceMessage),
        Rule("EconomySeats", "Number", "Required when economy price is provided.", NonNegativeClassSeatsMessage),
        Rule("BusinessPrice", "Money", "Required when business seats are provided.", PositiveClassPriceMessage),
        Rule("BusinessSeats", "Number", "Required when business price is provided.", NonNegativeClassSeatsMessage),
        Rule("FirstClassPrice", "Money", "Required when first class seats are provided.", PositiveClassPriceMessage),
        Rule("FirstClassSeats", "Number", "Required when first class price is provided.", NonNegativeClassSeatsMessage)
    ];

    public static string PriceField(FlightClass flightClass) => $"{ClassPrefix(flightClass)}Price";

    public static string SeatsField(FlightClass flightClass) => $"{ClassPrefix(flightClass)}Seats";

    private static string ClassPrefix(FlightClass flightClass) =>
        flightClass == FlightClass.FirstClass ? "FirstClass" : flightClass.ToString();

    private static ValidationRuleDescription Rule(string field, string type, params string[] constraints) =>
        new()
        {
            Field = field,
            Type = type,
            Constraints = constraints.ToList()
        };
}
