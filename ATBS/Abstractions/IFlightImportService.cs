using ATBS.DTOs;

namespace ATBS.Abstractions;

public interface IFlightImportService
{
    ImportResult PreviewImport(string csvPath);
    ImportResult Import(string csvPath);
}
