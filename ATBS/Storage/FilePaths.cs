namespace ATBS.Storage;

public sealed class FilePaths(string dataDirectory)
{
    private string DataDirectory { get; } = dataDirectory;
    public string FlightsPath => Path.Combine(DataDirectory, "flights.json");
    public string BookingsPath => Path.Combine(DataDirectory, "bookings.json");
    public string PassengersPath => Path.Combine(DataDirectory, "passengers.json");
}
