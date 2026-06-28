using ATBS.Models;

namespace ATBS.DTOs;

public sealed class ImportResult
{
    public List<Flight> ValidFlights { get; init; } = [];
    public List<ValidationError> Errors { get; init; } = [];
    public int TotalRows { get; init; }
    public int ValidRows => ValidFlights.Count;
    public int FailedRows => Errors.Select(error => error.RowNumber).Distinct().Count();
    public bool HasErrors => Errors.Count > 0;
}
