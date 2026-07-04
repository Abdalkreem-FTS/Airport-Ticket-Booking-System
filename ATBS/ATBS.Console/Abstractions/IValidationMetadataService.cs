using ATBS.Console.DTOs;

namespace ATBS.Console.Abstractions;

public interface IValidationMetadataService
{
    IReadOnlyList<ValidationRuleDescription> GetFlightValidationRules();
}