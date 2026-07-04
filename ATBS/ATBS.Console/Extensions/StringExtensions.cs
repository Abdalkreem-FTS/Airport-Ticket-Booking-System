namespace ATBS.Console.Extensions;

public static class StringExtensions
{
    public static bool TextEquals(this string currentValue, string requestedValue) =>
        string.Equals(currentValue.Trim(), requestedValue.Trim(), StringComparison.OrdinalIgnoreCase);
}