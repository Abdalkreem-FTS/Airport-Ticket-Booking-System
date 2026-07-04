using ATBS.Console.Abstractions;
using ATBS.Console.Models;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Storage.Repositories;

public sealed class FlightRepository(ITransactionalFileStorage storage) : IFlightRepository
{
    public async Task<Result<IReadOnlyList<Flight>>> GetAllAsync() => await storage.LoadAsync<Flight>(TransactionFile.Flights);

    public async Task<Result<Flight>> GetByIdAsync(Guid id)
    {
        var flightsResult = await GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var flight = flightsResult.Value.FirstOrDefault(flight => flight.Id == id);
        return flight is null
            ? Error.NotFound("Flights.NotFound", "Flight was not found.")
            : flight;
    }

    public async Task<Result<Created>> AddAsync(Flight flight)
    {
        var flightsResult = await GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var flights = flightsResult.Value.ToList();
        
        flights.Add(flight);
        
        var saveResult = await storage.SaveAsync(TransactionFile.Flights, flights);
        return saveResult.IsError ? saveResult.Errors : Result.Created;
    }

    public async Task<Result<Created>> AddRangeAsync(IEnumerable<Flight> flights)
    {
        var flightsResult = await GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var savedFlights = flightsResult.Value.ToList();
        
        savedFlights.AddRange(flights);
        
        var saveResult = await storage.SaveAsync(TransactionFile.Flights, savedFlights);
        
        return saveResult.IsError ? saveResult.Errors : Result.Created;
    }

    public async Task<Result<Updated>> UpdateAsync(Flight flight)
    {
        var flightsResult = await GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var flights = flightsResult.Value.ToList();
        var index = flights.FindIndex(savedFlight => savedFlight.Id == flight.Id);
        
        if (index < 0)
        {
            return Error.NotFound("Flights.NotFound", "Flight was not found.");
        }

        flights[index] = flight;
        var saveResult = await storage.SaveAsync(TransactionFile.Flights, flights);
        return saveResult.IsError ? saveResult.Errors : Result.Updated;
    }

    public async Task<Result<Deleted>> DeleteAsync(Guid id)
    {
        var flightsResult = await GetAllAsync();
        if (flightsResult.IsError)
        {
            return flightsResult.Errors;
        }

        var flights = flightsResult.Value.Where(flight => flight.Id != id).ToList();
        
        var saveResult = await storage.SaveAsync(TransactionFile.Flights, flights);
        return saveResult.IsError ? saveResult.Errors : Result.Deleted;
    }
}