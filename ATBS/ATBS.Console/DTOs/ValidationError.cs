namespace ATBS.Console.DTOs;

public sealed class ValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? AttemptedValue { get; init; }
    public int? RowNumber { get; init; }
}