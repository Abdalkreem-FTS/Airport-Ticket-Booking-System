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
        Assert.Equal(expected, FlightValidationRules.PriceField(flightClass));
    }

    [Theory]
    [InlineData(FlightClass.Economy, "EconomySeats")]
    [InlineData(FlightClass.Business, "BusinessSeats")]
    [InlineData(FlightClass.FirstClass, "FirstClassSeats")]
    public void SeatsField_UsesClassPrefix(FlightClass flightClass, string expected)
    {
        Assert.Equal(expected, FlightValidationRules.SeatsField(flightClass));
    }

    [Fact]
    public void Descriptions_CoverEveryImportColumn()
    {
        var fields = FlightValidationRules.Descriptions.Select(rule => rule.Field).ToList();

        Assert.Contains("FlightNumber", fields);
        Assert.Contains("Capacity", fields);
        Assert.Contains("FirstClassSeats", fields);
        Assert.All(FlightValidationRules.Descriptions, rule => Assert.NotEmpty(rule.Constraints));
    }
}
