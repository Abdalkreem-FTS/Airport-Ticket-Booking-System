using ATBS.DTOs;

namespace ATBS.Abstractions;

/// <summary>
/// Validates a model and returns structured errors instead of throwing.
/// </summary>
public interface IValidator<in T>
{
    IReadOnlyList<ValidationError> Validate(T model);
}
