namespace ATBS.Console.DTOs;

public sealed class ValidationRuleDescription
{
    public string Field { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public List<string> Constraints { get; init; } = [];
}