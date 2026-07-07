using ATBS.Console.Results;

namespace ATBS.Tests.Results;

public sealed class ErrorTests
{
    [Fact]
    public void Factory_UsesMethodName_AsDefaultCode()
    {
        Assert.Equal("Failure", Error.Failure().Code);
        Assert.Equal("NotFound", Error.NotFound().Code);
    }

    [Fact]
    public void Factory_PreservesCustomCodeAndDescription()
    {
        var error = Error.Validation("Flights.BadDate", "Departure date must be in the future.");

        Assert.Equal("Flights.BadDate", error.Code);
        Assert.Equal("Departure date must be in the future.", error.Description);
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Unexpected)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void Factory_AssignsMatchingErrorType(ErrorType expected)
    {
        var error = expected switch
        {
            ErrorType.Failure => Error.Failure(),
            ErrorType.Unexpected => Error.Unexpected(),
            ErrorType.Validation => Error.Validation(),
            ErrorType.Conflict => Error.Conflict(),
            ErrorType.NotFound => Error.NotFound(),
            ErrorType.Unauthorized => Error.Unauthorized(),
            ErrorType.Forbidden => Error.Forbidden(),
            _ => throw new ArgumentOutOfRangeException(nameof(expected))
        };

        Assert.Equal(expected, error.Type);
    }

    [Fact]
    public void Create_MapsIntegerToErrorType()
    {
        var error = Error.Create((int)ErrorType.NotFound, "Some.Code", "description");

        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("Some.Code", error.Code);
        Assert.Equal("description", error.Description);
    }
}
