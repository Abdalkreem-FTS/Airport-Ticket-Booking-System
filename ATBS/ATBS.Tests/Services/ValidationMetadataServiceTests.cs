using ATBS.Console.Services;
using ATBS.Console.Validation;

namespace ATBS.Tests.Services;

public sealed class ValidationMetadataServiceTests
{
    [Fact]
    public void GetFlightValidationRules_ReturnsSharedRuleDescriptions()
    {
        var rules = new ValidationMetadataService().GetFlightValidationRules();

        rules.Should().NotBeEmpty();
        rules.Should().BeSameAs(FlightValidationRules.Descriptions);
        rules.Should().Contain(rule => rule.Field == "FlightNumber");
    }
}
