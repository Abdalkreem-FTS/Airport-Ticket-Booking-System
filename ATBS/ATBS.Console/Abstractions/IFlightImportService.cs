using ATBS.Console.DTOs;
using ATBS.Console.Results;

namespace ATBS.Console.Abstractions;

/// <summary>
/// Imports flight data from CSV files and reports validation results.
/// </summary>
public interface IFlightImportService
{
    Task<Result<ImportResult>> PreviewImportAsync(string csvPath);
    Task<Result<ImportResult>> ImportAsync(string csvPath);
}