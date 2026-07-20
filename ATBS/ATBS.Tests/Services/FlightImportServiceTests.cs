using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Results;
using ATBS.Console.Services;
using ATBS.Tests.TestSupport;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace ATBS.Tests.Services;

public sealed class FlightImportServiceTests : IDisposable
{
    private const string ValidHeader =
        "FlightNumber,DepartureCountry,DestinationCountry,DepartureDate,DepartureAirport,ArrivalAirport," +
        "Capacity,EconomyPrice,EconomySeats,BusinessPrice,BusinessSeats,FirstClassPrice,FirstClassSeats";

    private const string ValidRow = "RJ100,Jordan,France,2030-01-01,AMM,CDG,100,100,10,,,,";

    private readonly Mock<IFlightRepository> _flights = new();
    private readonly Mock<IValidator<Flight>> _validator = new();
    private readonly Mock<IFileTransactionFactory> _factory = new();
    private readonly string _tempDirectory;

    public FlightImportServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "atbs_import_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        ValidatorReturns();
        _factory.RunsWorkInline<ImportResult>();
    }

    private FlightImportService CreateService() => new(_flights.Object, _validator.Object, _factory.Object);

    private void ValidatorReturns(params ValidationFailure[] failures) =>
        _validator.Setup(v => v.ValidateAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(failures));

    private string WriteCsv(params string[] lines)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.csv");
        File.WriteAllLines(path, lines);
        return path;
    }

    [Fact]
    public async Task PreviewImportAsync_ReportsFileError_WhenFileMissing()
    {
        var path = Path.Combine(_tempDirectory, "does-not-exist.csv");

        var result = await CreateService().PreviewImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.Errors.Should().Contain(error => error.Field == "File");
        result.Value.ValidFlights.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewImportAsync_ReturnsError_WhenRequiredColumnsMissing()
    {
        var path = WriteCsv("FlightNumber", "RJ100");

        var result = await CreateService().PreviewImportAsync(path);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Import.MissingColumns");
    }

    [Fact]
    public async Task PreviewImportAsync_ReturnsEmptyResult_WhenOnlyHeaderPresent()
    {
        var path = WriteCsv(ValidHeader);

        var result = await CreateService().PreviewImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRows.Should().Be(0);
        result.Value.ValidFlights.Should().BeEmpty();
        result.Value.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewImportAsync_MapsAndKeeps_ValidRow()
    {
        var path = WriteCsv(ValidHeader, ValidRow);

        var result = await CreateService().PreviewImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRows.Should().Be(1);
        var flight = result.Value.ValidFlights.Should().ContainSingle().Which;
        flight.FlightNumber.Should().Be("RJ100");
        flight.DepartureCountry.Should().Be("Jordan");
        flight.Capacity.Should().Be(100);
        result.Value.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewImportAsync_RecordsParseError_AndSkipsRow_WhenDateIsInvalid()
    {
        const string badRow = "RJ100,Jordan,France,not-a-date,AMM,CDG,100,100,10,,,,";
        var path = WriteCsv(ValidHeader, badRow);

        var result = await CreateService().PreviewImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.ValidFlights.Should().BeEmpty();
        result.Value.Errors.Should().Contain(error => error.Field == "DepartureDate");
        _validator.Verify(v => v.ValidateAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreviewImportAsync_RecordsValidationErrors_AndExcludesRow()
    {
        ValidatorReturns(new ValidationFailure("Capacity", "Capacity must be greater than zero."));
        var path = WriteCsv(ValidHeader, ValidRow);

        var result = await CreateService().PreviewImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.ValidFlights.Should().BeEmpty();
        result.Value.Errors.Should().Contain(error => error.Field == "Capacity");
    }

    [Fact]
    public async Task ImportAsync_SavesValidFlights_WithinTransaction()
    {
        _flights.Setup(f => f.AddRangeAsync(It.IsAny<IEnumerable<Flight>>())).ReturnsAsync(Builders.Ok(Result.Created));
        var path = WriteCsv(ValidHeader, ValidRow);

        var result = await CreateService().ImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.ValidFlights.Should().ContainSingle();
        _flights.Verify(f => f.AddRangeAsync(It.Is<IEnumerable<Flight>>(flights => flights.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_DoesNotPersist_WhenThereAreNoValidFlights()
    {
        const string badRow = "RJ100,Jordan,France,not-a-date,AMM,CDG,100,100,10,,,,";
        var path = WriteCsv(ValidHeader, badRow);

        var result = await CreateService().ImportAsync(path);

        result.IsSuccess.Should().BeTrue();
        result.Value.ValidFlights.Should().BeEmpty();
        _flights.Verify(f => f.AddRangeAsync(It.IsAny<IEnumerable<Flight>>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_PropagatesError_WhenPreviewFails()
    {
        var path = WriteCsv("FlightNumber", "RJ100");

        var result = await CreateService().ImportAsync(path);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Import.MissingColumns");
        _flights.Verify(f => f.AddRangeAsync(It.IsAny<IEnumerable<Flight>>()), Times.Never);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
