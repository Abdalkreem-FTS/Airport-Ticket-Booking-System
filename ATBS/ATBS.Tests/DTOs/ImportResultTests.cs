using ATBS.Console.DTOs;
using ATBS.Tests.TestSupport;

namespace ATBS.Tests.DTOs;

public sealed class ImportResultTests
{
    [Fact]
    public void ValidRows_ReflectsValidFlightCount()
    {
        var result = new ImportResult
        {
            ValidFlights = [Builders.NewFlight(), Builders.NewFlight()],
            TotalRows = 3
        };

        result.ValidRows.Should().Be(2);
    }

    [Fact]
    public void FailedRows_CountsDistinctRowNumbers()
    {
        var result = new ImportResult
        {
            Errors =
            [
                new ValidationError { Field = "Capacity", Message = "m", RowNumber = 2 },
                new ValidationError { Field = "EconomyPrice", Message = "m", RowNumber = 2 },
                new ValidationError { Field = "DepartureDate", Message = "m", RowNumber = 5 }
            ]
        };

        result.FailedRows.Should().Be(2);
    }

    [Fact]
    public void HasErrors_IsTrue_WhenErrorsPresent()
    {
        var result = new ImportResult
        {
            Errors = [new ValidationError { Field = "File", Message = "missing" }]
        };

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void HasErrors_IsFalse_WhenNoErrors()
    {
        var result = new ImportResult { ValidFlights = [Builders.NewFlight()] };

        result.HasErrors.Should().BeFalse();
    }
}
