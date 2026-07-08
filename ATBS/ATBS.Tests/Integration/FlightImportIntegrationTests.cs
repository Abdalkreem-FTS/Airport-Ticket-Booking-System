using System.Globalization;
using ATBS.Tests.TestSupport;

namespace ATBS.Tests.Integration;

/// <summary>
/// Drives the CSV import through the real pipeline: file read, row mapping, the actual <c>FlightValidator</c>
/// (no stub), a SERIALIZABLE transaction, and JSON persistence. Assertions re-read the flights table so we
/// verify what was committed, not just the returned preview.
/// </summary>
public sealed class FlightImportIntegrationTests
{
    private const string Header =
        "FlightNumber,DepartureCountry,DestinationCountry,DepartureDate,DepartureAirport,ArrivalAirport," +
        "Capacity,EconomyPrice,EconomySeats,BusinessPrice,BusinessSeats,FirstClassPrice,FirstClassSeats";

    // Computed rather than hard-coded so the "departure date must be today or later" rule keeps passing over time.
    private static readonly string FutureDate =
        DateTimeOffset.UtcNow.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public async Task ImportAsync_PersistsOnlyValidRows_ThroughRealValidatorAndTransaction()
    {
        await using var harness = new IntegrationTestHarness();
        var csvPath = WriteCsv(harness,
            Header,
            $"RJ100,Jordan,France,{FutureDate},AMM,CDG,100,100,10,,,,",
            $"RJ200,Egypt,Spain,{FutureDate},CAI,MAD,150,120,20,400,5,,",
            // Invalid: destination equals departure country AND arrival equals departure airport.
            $"BAD1,Spain,Spain,{FutureDate},BCN,BCN,100,100,10,,,,");

        var result = await harness.FlightImportService.ImportAsync(csvPath);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalRows);
        Assert.Equal(2, result.Value.ValidFlights.Count);
        Assert.NotEmpty(result.Value.Errors);

        // The two valid flights are actually on disk with their mapped values.
        var persisted = (await harness.FlightRepository.GetAllAsync()).Value;
        Assert.Equal(2, persisted.Count);
        Assert.Contains(persisted, flight => flight.FlightNumber == "RJ100" && flight.Capacity == 100);
        Assert.Equal(2, Assert.Single(persisted, flight => flight.FlightNumber == "RJ200").ClassPrices.Count);

        Assert.Empty(harness.PendingTransactionLogFiles);
    }

    [Fact]
    public async Task ImportAsync_WhenEveryRowIsInvalid_PersistsNothing()
    {
        await using var harness = new IntegrationTestHarness();
        var csvPath = WriteCsv(harness,
            Header,
            $"BAD1,Spain,Spain,{FutureDate},BCN,BCN,100,100,10,,,,");

        var result = await harness.FlightImportService.ImportAsync(csvPath);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.ValidFlights);
        Assert.NotEmpty(result.Value.Errors);
        Assert.Empty((await harness.FlightRepository.GetAllAsync()).Value);
    }

    [Fact]
    public async Task PreviewImportAsync_ValidatesWithoutPersisting()
    {
        await using var harness = new IntegrationTestHarness();
        var csvPath = WriteCsv(harness,
            Header,
            $"RJ100,Jordan,France,{FutureDate},AMM,CDG,100,100,10,,,,");

        var preview = await harness.FlightImportService.PreviewImportAsync(csvPath);

        Assert.True(preview.IsSuccess);
        Assert.Single(preview.Value.ValidFlights);
        // Preview is read-only: nothing was written to the flights table.
        Assert.Empty((await harness.FlightRepository.GetAllAsync()).Value);
    }

    private static string WriteCsv(IntegrationTestHarness harness, params string[] lines)
    {
        var path = Path.Combine(harness.DataDirectory, $"import_{Guid.NewGuid():N}.csv");
        File.WriteAllLines(path, lines);
        return path;
    }
}
