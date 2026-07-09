using ATBS.Console.Results;

namespace ATBS.Tests.Results;

public sealed class ErrorTests
{
    [Fact]
    public void Factory_UsesMethodName_AsDefaultCode()
    {
        Error.Failure().Code.Should().Be("Failure");
        Error.NotFound().Code.Should().Be("NotFound");
    }

    [Fact]
    public void Factory_PreservesCustomCodeAndDescription()
    {
        var error = Error.Validation("Flights.BadDate", "Departure date must be in the future.");

        error.Code.Should().Be("Flights.BadDate");
        error.Description.Should().Be("Departure date must be in the future.");
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

        error.Type.Should().Be(expected);
    }

    [Fact]
    public void Create_MapsIntegerToErrorType()
    {
        var error = Error.Create((int)ErrorType.NotFound, "Some.Code", "description");

        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("Some.Code");
        error.Description.Should().Be("description");
    }
}
