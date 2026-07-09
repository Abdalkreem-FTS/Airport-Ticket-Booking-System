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

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRows.Should().Be(3);
        result.Value.ValidFlights.Should().HaveCount(2);
        result.Value.Errors.Should().NotBeEmpty();

        // The two valid flights are actually on disk with their mapped values.
        var persisted = (await harness.FlightRepository.GetAllAsync()).Value;
        persisted.Should().HaveCount(2);
        persisted.Should().Contain(flight => flight.FlightNumber == "RJ100" && flight.Capacity == 100);
        persisted.Should().ContainSingle(flight => flight.FlightNumber == "RJ200").Which.ClassPrices.Should().HaveCount(2);

        harness.PendingTransactionLogFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WhenEveryRowIsInvalid_PersistsNothing()
    {
        await using var harness = new IntegrationTestHarness();
        var csvPath = WriteCsv(harness,
            Header,
            $"BAD1,Spain,Spain,{FutureDate},BCN,BCN,100,100,10,,,,");

        var result = await harness.FlightImportService.ImportAsync(csvPath);

        result.IsSuccess.Should().BeTrue();
        result.Value.ValidFlights.Should().BeEmpty();
        result.Value.Errors.Should().NotBeEmpty();
        (await harness.FlightRepository.GetAllAsync()).Value.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewImportAsync_ValidatesWithoutPersisting()
    {
        await using var harness = new IntegrationTestHarness();
        var csvPath = WriteCsv(harness,
            Header,
            $"RJ100,Jordan,France,{FutureDate},AMM,CDG,100,100,10,,,,");

        var preview = await harness.FlightImportService.PreviewImportAsync(csvPath);

        preview.IsSuccess.Should().BeTrue();
        preview.Value.ValidFlights.Should().ContainSingle();
        // Preview is read-only: nothing was written to the flights table.
        (await harness.FlightRepository.GetAllAsync()).Value.Should().BeEmpty();
    }

    private static string WriteCsv(IntegrationTestHarness harness, params string[] lines)
    {
        var path = Path.Combine(harness.DataDirectory, $"import_{Guid.NewGuid():N}.csv");
        File.WriteAllLines(path, lines);
        return path;
    }
}
