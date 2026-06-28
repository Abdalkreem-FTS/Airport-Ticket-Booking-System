using ATBS.Abstractions;
using ATBS.Models;

namespace ATBS.Storage.Repositories;

public sealed class FlightRepository(IFileStorage storage, FilePaths filePaths) : IFlightRepository
{
    public IReadOnlyList<Flight> GetAll() => storage.Load<Flight>(filePaths.FlightsPath);

    public Flight? GetById(Guid id) => GetAll().FirstOrDefault(flight => flight.Id == id);

    public void Add(Flight flight)
    {
        var flights = GetAll().ToList();
        
        flights.Add(flight);
        
        storage.Save(filePaths.FlightsPath, flights);
    }

    public void AddRange(IEnumerable<Flight> flights)
    {
        var savedFlights = GetAll().ToList();
        
        savedFlights.AddRange(flights);
        
        storage.Save(filePaths.FlightsPath, savedFlights);
    }

    public void Update(Flight flight)
    {
        var flights = GetAll().ToList();
        var index = flights.FindIndex(savedFlight => savedFlight.Id == flight.Id);
        
        if (index < 0)
        {
            throw new InvalidOperationException("Flight was not found.");
        }

        flights[index] = flight;
        storage.Save(filePaths.FlightsPath, flights);
    }

    public void Delete(Guid id)
    {
        var flights = GetAll().Where(flight => flight.Id != id).ToList();
        
        storage.Save(filePaths.FlightsPath, flights);
    }
}
