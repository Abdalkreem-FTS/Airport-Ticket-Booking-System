using ATBS.Console.Services;
using ATBS.Console.Validation;

namespace ATBS.Tests.Services;

public sealed class ValidationMetadataServiceTests
{
    [Fact]
    public void GetFlightValidationRules_ReturnsSharedRuleDescriptions()
    {
        var rules = new ValidationMetadataService().GetFlightValidationRules();

        Assert.NotEmpty(rules);
        Assert.Same(FlightValidationRules.Descriptions, rules);
        Assert.Contains(rules, rule => rule.Field == "FlightNumber");
    }
}
