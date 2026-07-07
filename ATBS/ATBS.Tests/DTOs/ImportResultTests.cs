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

        Assert.Equal(2, result.ValidRows);
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

        Assert.Equal(2, result.FailedRows);
    }

    [Fact]
    public void HasErrors_IsTrue_WhenErrorsPresent()
    {
        var result = new ImportResult
        {
            Errors = [new ValidationError { Field = "File", Message = "missing" }]
        };

        Assert.True(result.HasErrors);
    }

    [Fact]
    public void HasErrors_IsFalse_WhenNoErrors()
    {
        var result = new ImportResult { ValidFlights = [Builders.NewFlight()] };

        Assert.False(result.HasErrors);
    }
}
