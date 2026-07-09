using ATBS.Console.Results;

namespace ATBS.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        Result<int> result = Error.Conflict("Seat.Taken", "already booked");

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Seat.Taken");
        result.TopError.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void ImplicitConversion_FromErrorList_ProducesFailure()
    {
        Result<int> result = new List<Error>
        {
            Error.Validation("A", "a"), 
            Error.Validation("B", "b")
        };

        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.TopError.Code.Should().Be("A");
    }

    [Fact]
    public void EmptyErrorList_IsNormalizedToASingleError()
    {
        Result<int> result = new List<Error>();

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Result.EmptyErrors");
    }

    [Fact]
    public void From_NullValue_ProducesFailure()
    {
        var result = Result<string>.From(null!);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Result.NullValue");
    }

    [Fact]
    public void Match_InvokesOnValue_ForSuccess()
    {
        Result<int> result = 7;

        var text = result.Match(value => $"value:{value}", _ => "error");

        text.Should().Be("value:7");
    }

    [Fact]
    public void Match_InvokesOnError_ForFailure()
    {
        Result<int> result = Error.Failure("X", "boom");

        var text = result.Match(_ => "value", errors => $"errors:{errors.Count}");

        text.Should().Be("errors:1");
    }

    [Fact]
    public void Value_OnFailure_IsDefault_AndErrorsAvailable()
    {
        Result<int> result = Error.NotFound("Missing", "gone");

        result.Value.Should().Be(0);
        result.Errors.Should().ContainSingle();
    }
}
