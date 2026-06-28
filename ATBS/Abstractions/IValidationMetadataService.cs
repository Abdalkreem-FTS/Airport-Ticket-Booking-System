using ATBS.DTOs;

namespace ATBS.Abstractions;

public interface IValidationMetadataService
{
    IReadOnlyList<ValidationRuleDescription> GetFlightValidationRules();
}
