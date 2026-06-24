using System.Globalization;
using ATBS.Abstractions;
using ATBS.DTOs;
using ATBS.Models;
using ATBS.Models.Enums;

namespace ATBS.Services;

/// <summary>
/// Reads flight CSV files, maps rows to flights, validates them, and saves valid rows.
/// </summary>
public sealed class FlightImportService(IFlightRepository flightRepository, IValidator<Flight> flightValidator)
    : IFlightImportService
{
    public ImportResult PreviewImport(string csvPath)
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

        var rows = ReadRows(csvPath);
        var validFlights = new List<Flight>();
        var errors = new List<ValidationError>();

        foreach (var row in rows)
        {
            var mappedFlight = MapRow(row, errors);
            if (mappedFlight is null)
            {
                continue;
            }

            var validationErrors = flightValidator.Validate(mappedFlight)
                .Select(error => WithRowNumber(error, row.RowNumber));

            errors.AddRange(validationErrors);

            if (errors.All(error => error.RowNumber != row.RowNumber))
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

    public ImportResult Import(string csvPath)
    {
        var result = PreviewImport(csvPath);
        if (result.ValidFlights.Count > 0)
        {
            flightRepository.AddRange(result.ValidFlights);
        }

        return result;
    }

    private static List<ImportFlightRow> ReadRows(string csvPath)
    {
        var lines = File.ReadAllLines(csvPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count <= 1)
        {
            return [];
        }

        return lines.Skip(1)
            .Select((line, index) => ToRow(ParseCsvLine(line), index + 2))
            .ToList();
    }

    private static ImportFlightRow ToRow(IReadOnlyList<string> columns, int rowNumber) =>
        new()
        {
            RowNumber = rowNumber,
            FlightNumber = Get(columns, 0),
            DepartureCountry = Get(columns, 1),
            DestinationCountry = Get(columns, 2),
            DepartureDate = Get(columns, 3),
            DepartureAirport = Get(columns, 4),
            ArrivalAirport = Get(columns, 5),
            Capacity = Get(columns, 6),
            EconomyPrice = Get(columns, 7),
            EconomySeats = Get(columns, 8),
            BusinessPrice = Get(columns, 9),
            BusinessSeats = Get(columns, 10),
            FirstClassPrice = Get(columns, 11),
            FirstClassSeats = Get(columns, 12)
        };

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

    private static ValidationError WithRowNumber(ValidationError error, int rowNumber) =>
        new()
        {
            Field = error.Field,
            Message = error.Message,
            AttemptedValue = error.AttemptedValue,
            RowNumber = rowNumber
        };

    private static string Get(IReadOnlyList<string> columns, int index) =>
        index < columns.Count ? columns[index].Trim() : string.Empty;

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            switch (character)
            {
                case '"' when inQuotes && index + 1 < line.Length && line[index + 1] == '"':
                    current.Append('"');
                    index++;
                    continue;
                case '"':
                    inQuotes = !inQuotes;
                    continue;
                case ',' when !inQuotes:
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                default:
                    current.Append(character);
                    break;
            }
        }

        values.Add(current.ToString());
        
        return values;
    }
}
