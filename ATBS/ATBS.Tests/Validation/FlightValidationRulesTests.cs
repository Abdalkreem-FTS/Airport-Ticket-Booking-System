using ATBS.Console.Models.Enums;
using ATBS.Console.Validation;

namespace ATBS.Tests.Validation;

public sealed class FlightValidationRulesTests
{
    [Theory]
    [InlineData(FlightClass.Economy, "EconomyPrice")]
    [InlineData(FlightClass.Business, "BusinessPrice")]
    [InlineData(FlightClass.FirstClass, "FirstClassPrice")]
    public void PriceField_UsesClassPrefix(FlightClass flightClass, string expected)
    {
        FlightValidationRules.PriceField(flightClass).Should().Be(expected);
    }

    [Theory]
    [InlineData(FlightClass.Economy, "EconomySeats")]
    [InlineData(FlightClass.Business, "BusinessSeats")]
    [InlineData(FlightClass.FirstClass, "FirstClassSeats")]
    public void SeatsField_UsesClassPrefix(FlightClass flightClass, string expected)
    {
        FlightValidationRules.SeatsField(flightClass).Should().Be(expected);
    }

    [Fact]
    public void Descriptions_CoverEveryImportColumn()
    {
        var fields = FlightValidationRules.Descriptions.Select(rule => rule.Field).ToList();

        fields.Should().Contain("FlightNumber");
        fields.Should().Contain("Capacity");
        fields.Should().Contain("FirstClassSeats");
        FlightValidationRules.Descriptions.Should().OnlyContain(rule => rule.Constraints.Any());
    }
}
