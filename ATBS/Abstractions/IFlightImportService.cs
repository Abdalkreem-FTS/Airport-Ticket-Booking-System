using ATBS.DTOs;

namespace ATBS.Abstractions;

/// <summary>
/// Imports flight data from CSV files and reports validation results.
/// </summary>
public interface IFlightImportService
{
    ImportResult PreviewImport(string csvPath);
    ImportResult Import(string csvPath);
}
