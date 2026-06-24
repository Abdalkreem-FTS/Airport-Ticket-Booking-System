using ATBS.Abstractions;
using ATBS.DTOs;

namespace ATBS.Services;

/// <summary>
/// Provides human-readable validation rules for manager import guidance.
/// </summary>
public sealed class ValidationMetadataService : IValidationMetadataService
{
    public IReadOnlyList<ValidationRuleDescription> GetFlightValidationRules() =>
    [
        Rule("FlightNumber", "Free Text", "Required", "Unique business identifier recommended"),
        Rule("DepartureCountry", "Free Text", "Required"),
        Rule("DestinationCountry", "Free Text", "Required", "Must differ from departure country"),
        Rule("DepartureDate", "Date Time", "Required", "Allowed range: today to future"),
        Rule("DepartureAirport", "Free Text", "Required"),
        Rule("ArrivalAirport", "Free Text", "Required", "Must differ from departure airport"),
        Rule("Capacity", "Number", "Required", "Greater than zero"),
        Rule("EconomyPrice", "Money", "Optional", "Required when economy seats are provided", "Greater than zero"),
        Rule("EconomySeats", "Number", "Optional", "Zero or greater"),
        Rule("BusinessPrice", "Money", "Optional", "Required when business seats are provided", "Greater than zero"),
        Rule("BusinessSeats", "Number", "Optional", "Zero or greater"),
        Rule("FirstClassPrice", "Money", "Optional", "Required when first class seats are provided", "Greater than zero"),
        Rule("FirstClassSeats", "Number", "Optional", "Zero or greater")
    ];

    private static ValidationRuleDescription Rule(string field, string type, params string[] constraints) =>
        new()
        {
            Field = field,
            Type = type,
            Constraints = constraints.ToList()
        };
}
