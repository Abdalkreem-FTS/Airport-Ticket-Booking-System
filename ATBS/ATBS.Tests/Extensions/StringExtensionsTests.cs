using ATBS.Console.Extensions;

namespace ATBS.Tests.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("Palestine", "Palestine")]
    [InlineData("Palestine", "palestine")]
    [InlineData("  Palestine  ", "Palestine")]
    [InlineData("PNA", "pna")]
    public void TextEquals_IsTrue_ForCaseInsensitiveTrimmedMatches(string left, string right)
    {
        left.TextEquals(right).Should().BeTrue();
    }

    [Theory]
    [InlineData("Jordan", "Egypt")]
    [InlineData("AMM", "ENA")]
    [InlineData("", "Jordan")]
    public void TextEquals_IsFalse_ForDifferentText(string left, string right)
    {
        left.TextEquals(right).Should().BeFalse();
    }
}
