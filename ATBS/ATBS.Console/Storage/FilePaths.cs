namespace ATBS.Console.Storage;

public sealed class FilePaths(string dataDirectory)
{
    public string DataDirectory { get; } = dataDirectory;
    public string SeedDirectory => Path.Combine(DataDirectory, "Seed");
    public string FlightsPath => Path.Combine(DataDirectory, "flights.json");
    public string BookingsPath => Path.Combine(DataDirectory, "bookings.json");
    public string PassengersPath => Path.Combine(DataDirectory, "passengers.json");
}