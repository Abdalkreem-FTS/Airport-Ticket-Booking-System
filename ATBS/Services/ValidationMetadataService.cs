using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Validation;

namespace ATBS.Services;

/// <summary>
/// Provides human-readable validation rules for manager import guidance.
/// </summary>
public sealed class ValidationMetadataService : IValidationMetadataService
{
    public IReadOnlyList<ValidationRuleDescription> GetFlightValidationRules() =>
        FlightValidationRules.Descriptions;
}
