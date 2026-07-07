using ATBS.Console.Results;

namespace ATBS.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        Result<int> result = Error.Conflict("Seat.Taken", "already booked");

        Assert.True(result.IsError);
        Assert.Equal("Seat.Taken", result.TopError.Code);
        Assert.Equal(ErrorType.Conflict, result.TopError.Type);
    }

    [Fact]
    public void ImplicitConversion_FromErrorList_ProducesFailure()
    {
        Result<int> result = new List<Error>
        {
            Error.Validation("A", "a"), 
            Error.Validation("B", "b")
        };

        Assert.True(result.IsError);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("A", result.TopError.Code);
    }

    [Fact]
    public void EmptyErrorList_IsNormalizedToASingleError()
    {
        Result<int> result = new List<Error>();

        Assert.True(result.IsError);
        Assert.Equal("Result.EmptyErrors", result.TopError.Code);
    }

    [Fact]
    public void From_NullValue_ProducesFailure()
    {
        var result = Result<string>.From(null!);

        Assert.True(result.IsError);
        Assert.Equal("Result.NullValue", result.TopError.Code);
    }

    [Fact]
    public void Match_InvokesOnValue_ForSuccess()
    {
        Result<int> result = 7;

        var text = result.Match(value => $"value:{value}", _ => "error");

        Assert.Equal("value:7", text);
    }

    [Fact]
    public void Match_InvokesOnError_ForFailure()
    {
        Result<int> result = Error.Failure("X", "boom");

        var text = result.Match(_ => "value", errors => $"errors:{errors.Count}");

        Assert.Equal("errors:1", text);
    }

    [Fact]
    public void Value_OnFailure_IsDefault_AndErrorsAvailable()
    {
        Result<int> result = Error.NotFound("Missing", "gone");

        Assert.Equal(0, result.Value);
        Assert.Single(result.Errors);
    }
}
