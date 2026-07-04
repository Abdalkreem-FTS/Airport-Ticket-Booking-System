using System.Globalization;
using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;
using ATBS.Console.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace ATBS.Console.Services;

/// <summary>
/// Reads flight CSV files, maps rows to flights, validates them, and saves valid rows.
/// </summary>
public sealed class FlightImportService(
    IFlightRepository flightRepository,
    IValidator<Flight> flightValidator,
    IFileTransactionFactory transactionFactory)
    : IFlightImportService
{
    private static readonly string[] FlightImportColumns =
    [
        nameof(ImportFlightRow.FlightNumber),
        nameof(ImportFlightRow.DepartureCountry),
        nameof(ImportFlightRow.DestinationCountry),
        nameof(ImportFlightRow.DepartureDate),
        nameof(ImportFlightRow.DepartureAirport),
        nameof(ImportFlightRow.ArrivalAirport),
        nameof(ImportFlightRow.Capacity),
        nameof(ImportFlightRow.EconomyPrice),
        nameof(ImportFlightRow.EconomySeats),
        nameof(ImportFlightRow.BusinessPrice),
        nameof(ImportFlightRow.BusinessSeats),
        nameof(ImportFlightRow.FirstClassPrice),
        nameof(ImportFlightRow.FirstClassSeats)
    ];

    public async Task<Result<ImportResult>> PreviewImportAsync(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            return new ImportResult
            {
                Errors =
                [
                    new ValidationError
                    {
                        Field = "File",
                        Message = "CSV file was not found.",
                        AttemptedValue = csvPath
                    }
                ]
            };
        }

        var rowsResult = await ReadRowsAsync(csvPath);
        if (rowsResult.IsError)
        {
            return rowsResult.Errors;
        }

        var rows = rowsResult.Value;
        var validFlights = new List<Flight>();
        var errors = new List<ValidationError>();

        foreach (var row in rows)
        {
            var mappedFlight = MapRow(row, errors);
            if (mappedFlight is null)
            {
                continue;
            }

            var validationErrors = (await flightValidator.ValidateAsync(mappedFlight)).Errors
                .Select(error => ToValidationError(error, row.RowNumber))
                .ToList();

            errors.AddRange(validationErrors);

            if (validationErrors.Count == 0)
            {
                validFlights.Add(mappedFlight);
            }
        }

        return new ImportResult
        {
            ValidFlights = validFlights,
            Errors = errors,
            TotalRows = rows.Count
        };
    }

    public async Task<Result<ImportResult>> ImportAsync(string csvPath)
    {
        var result = await PreviewImportAsync(csvPath);
        if (result.IsError)
        {
            return result.Errors;
        }

        if (result.Value.ValidFlights.Count <= 0)
        {
            return result.Value;
        }

        var importResult = result.Value;

        return await transactionFactory.ExecuteAsync<ImportResult>(IsolationLevel.Serializable, async () =>
        {
            var saveResult = await flightRepository.AddRangeAsync(importResult.ValidFlights);
            return saveResult.IsError ? saveResult.Errors : importResult;
        });
    }

    private static async Task<Result<List<ImportFlightRow>>> ReadRowsAsync(string csvPath)
    {
        List<string> lines;
        try
        {
            lines = (await File.ReadAllLinesAsync(csvPath))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Error.Failure("Import.ReadFailed", $"Could not read CSV file: {exception.Message}");
        }

        if (lines.Count <= 1)
        {
            return new List<ImportFlightRow>();
        }

        var positionsResult = BuildColumnPositions(ParseCsvLine(lines[0]), FlightImportColumns);
        if (positionsResult.IsError)
        {
            return positionsResult.Errors;
        }

        return lines.Skip(1)
            .Select((line, index) => ToRow(ParseCsvLine(line), index + 2, positionsResult.Value, ToFlightImportRow))
            .ToList();
    }

    private static TRow ToRow<TRow>(
        IReadOnlyList<string> columns,
        int rowNumber,
        IReadOnlyDictionary<string, int> positions,
        Func<CsvRowReader, int, TRow> map)
    {
        return map(new CsvRowReader(columns, positions), rowNumber);
    }

    private static ImportFlightRow ToFlightImportRow(CsvRowReader row, int rowNumber) =>
        new()
        {
            RowNumber = rowNumber,
            FlightNumber = row.Get(nameof(ImportFlightRow.FlightNumber)),
            DepartureCountry = row.Get(nameof(ImportFlightRow.DepartureCountry)),
            DestinationCountry = row.Get(nameof(ImportFlightRow.DestinationCountry)),
            DepartureDate = row.Get(nameof(ImportFlightRow.DepartureDate)),
            DepartureAirport = row.Get(nameof(ImportFlightRow.DepartureAirport)),
            ArrivalAirport = row.Get(nameof(ImportFlightRow.ArrivalAirport)),
            Capacity = row.Get(nameof(ImportFlightRow.Capacity)),
            EconomyPrice = row.Get(nameof(ImportFlightRow.EconomyPrice)),
            EconomySeats = row.Get(nameof(ImportFlightRow.EconomySeats)),
            BusinessPrice = row.Get(nameof(ImportFlightRow.BusinessPrice)),
            BusinessSeats = row.Get(nameof(ImportFlightRow.BusinessSeats)),
            FirstClassPrice = row.Get(nameof(ImportFlightRow.FirstClassPrice)),
            FirstClassSeats = row.Get(nameof(ImportFlightRow.FirstClassSeats))
        };

    private static Result<IReadOnlyDictionary<string, int>> BuildColumnPositions(
        IReadOnlyList<string> headers,
        IEnumerable<string> requiredColumns)
    {
        var positions = headers
            .Select((header, index) => new { Name = NormalizeColumnName(header), Index = index })
            .Where(header => !string.IsNullOrWhiteSpace(header.Name))
            .GroupBy(header => header.Name)
            .ToDictionary(group => group.Key, group => group.First().Index);

        var missingColumns = requiredColumns
            .Where(column => !positions.ContainsKey(NormalizeColumnName(column)))
            .ToList();

        if (missingColumns.Count > 0)
        {
            return Error.Validation(
                "Import.MissingColumns",
                $"CSV file is missing required column(s): {string.Join(", ", missingColumns)}.");
        }

        return positions;
    }

    private static Flight? MapRow(ImportFlightRow row, List<ValidationError> errors)
    {
        var rowErrorsBefore = errors.Count;

        var departureDate = ParseDate(row.DepartureDate, nameof(row.DepartureDate), row.RowNumber, errors);
        var capacity = ParseInt(row.Capacity, nameof(row.Capacity), row.RowNumber, errors);
        var classPrices = new List<FlightClassPrice>();

        AddClassPrice(classPrices, FlightClass.Economy, row.EconomyPrice, row.EconomySeats, row.RowNumber, errors);
        AddClassPrice(classPrices, FlightClass.Business, row.BusinessPrice, row.BusinessSeats, row.RowNumber, errors);
        AddClassPrice(classPrices, FlightClass.FirstClass, row.FirstClassPrice, row.FirstClassSeats, row.RowNumber, errors);

        if (errors.Count > rowErrorsBefore)
        {
            return null;
        }

        return new Flight
        {
            FlightNumber = row.FlightNumber.Trim(),
            DepartureCountry = row.DepartureCountry.Trim(),
            DestinationCountry = row.DestinationCountry.Trim(),
            DepartureDate = departureDate!.Value,
            DepartureAirport = row.DepartureAirport.Trim(),
            ArrivalAirport = row.ArrivalAirport.Trim(),
            Capacity = capacity!.Value,
            ClassPrices = classPrices
        };
    }

    private static void AddClassPrice(
        List<FlightClassPrice> classPrices,
        FlightClass flightClass,
        string priceValue,
        string seatsValue,
        int rowNumber,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(priceValue) && string.IsNullOrWhiteSpace(seatsValue))
        {
            return;
        }

        var price = ParseDecimal(priceValue, $"{flightClass}Price", rowNumber, errors);
        var seats = ParseInt(seatsValue, $"{flightClass}Seats", rowNumber, errors);
        if (price is null || seats is null)
        {
            return;
        }

        classPrices.Add(new FlightClassPrice
        {
            Class = flightClass,
            Price = price.Value,
            AvailableSeats = seats.Value
        });
    }

    private static DateTimeOffset? ParseDate(string value, string field, int rowNumber, List<ValidationError> errors)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            return date;
        }

        AddParseError(field, "Value must be a valid date time.", value, rowNumber, errors);
        
        return null;
    }

    private static int? ParseInt(string value, string field, int rowNumber, List<ValidationError> errors)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        AddParseError(field, "Value must be a whole number.", value, rowNumber, errors);
        
        return null;
    }

    private static decimal? ParseDecimal(string value, string field, int rowNumber, List<ValidationError> errors)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        AddParseError(field, "Value must be a valid money amount.", value, rowNumber, errors);
        
        return null;
    }

    private static void AddParseError(
        string field,
        string message,
        string attemptedValue,
        int rowNumber,
        List<ValidationError> errors)
    {
        errors.Add(new ValidationError
        {
            Field = field,
            Message = message,
            AttemptedValue = attemptedValue,
            RowNumber = rowNumber
        });
    }

    private static ValidationError ToValidationError(ValidationFailure failure, int rowNumber)
    {
        var state = failure.CustomState as FlightValidator.FlightValidationState;

        return new ValidationError
        {
            Field = state?.Field ?? failure.PropertyName,
            Message = failure.ErrorMessage,
            AttemptedValue = state?.AttemptedValue ?? failure.AttemptedValue?.ToString(),
            RowNumber = rowNumber
        };
    }

    private static string Get(IReadOnlyList<string> columns, int index) => index < columns.Count ? columns[index].Trim() : string.Empty;

    private static string NormalizeColumnName(string columnName) =>
        columnName.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToUpperInvariant();

    private static List<string> ParseCsvLine(string line) => line.Split(',').ToList();

    private sealed class CsvRowReader(IReadOnlyList<string> columns, IReadOnlyDictionary<string, int> positions)
    {
        public string Get(string columnName)
        {
            return positions.TryGetValue(NormalizeColumnName(columnName), out var index)
                ? FlightImportService.Get(columns, index)
                : string.Empty;
        }
    }
}