using ATBS.Abstractions;
using ATBS.Models;

namespace ATBS.Storage.Repositories;

public sealed class FlightRepository(IFileStorage storage, FilePaths filePaths) : IFlightRepository
{
    public async Task<IEnumerable<Flight>> GetAllAsync() => await storage.LoadAsync<Flight>(filePaths.FlightsPath);

    public async Task<Flight?> GetByIdAsync(Guid id) => (await GetAllAsync()).FirstOrDefault(flight => flight.Id == id);

    public async Task AddAsync(Flight flight)
    {
        var flights = (await GetAllAsync()).ToList();
        
        flights.Add(flight);
        
        await storage.SaveAsync(filePaths.FlightsPath, flights);
    }

    public async Task AddRangeAsync(IEnumerable<Flight> flights)
    {
        var savedFlights = (await GetAllAsync()).ToList();
        
        savedFlights.AddRange(flights);
        
        await storage.SaveAsync(filePaths.FlightsPath, savedFlights);
    }

    public async Task UpdateAsync(Flight flight)
    {
        var flights = (await GetAllAsync()).ToList();
        var index = flights.FindIndex(savedFlight => savedFlight.Id == flight.Id);
        
        if (index < 0)
        {
            throw new InvalidOperationException("Flight was not found.");
        }

        flights[index] = flight;
        await storage.SaveAsync(filePaths.FlightsPath, flights);
    }

    public async Task DeleteAsync(Guid id)
    {
        var flights = (await GetAllAsync()).Where(flight => flight.Id != id).ToList();
        
        await storage.SaveAsync(filePaths.FlightsPath, flights);
    }
}
