using ATBS.DTOs;

namespace ATBS.Abstractions;

/// <summary>
/// Imports flight data from CSV files and reports validation results.
/// </summary>
public interface IFlightImportService
{
    Task<ImportResult> PreviewImportAsync(string csvPath);
    Task<ImportResult> ImportAsync(string csvPath);
}
