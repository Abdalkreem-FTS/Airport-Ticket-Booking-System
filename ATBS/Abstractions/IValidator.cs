using ATBS.DTOs;

namespace ATBS.Abstractions;

public interface IValidator<in T>
{
    IReadOnlyList<ValidationError> Validate(T model);
}
