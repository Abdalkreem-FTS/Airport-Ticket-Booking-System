using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Validation;

namespace ATBS.Console.Services;

/// <summary>
/// Provides human-readable validation rules for manager import guidance.
/// </summary>
public sealed class ValidationMetadataService : IValidationMetadataService
{
    public IReadOnlyList<ValidationRuleDescription> GetFlightValidationRules() =>
        FlightValidationRules.Descriptions;
}