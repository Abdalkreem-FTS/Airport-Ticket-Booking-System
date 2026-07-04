using ATBS.Console.Abstractions;
using ATBS.Console.Models;
using ATBS.Console.Results;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Storage.Repositories;

public sealed class PassengerRepository(ITransactionalFileStorage storage) : IPassengerRepository
{
    public async Task<Result<IReadOnlyList<Passenger>>> GetAllAsync() => await storage.LoadAsync<Passenger>(TransactionFile.Passengers);

    public async Task<Result<Passenger>> GetByIdAsync(Guid id)
    {
        var passengersResult = await GetAllAsync();
        if (passengersResult.IsError)
        {
            return passengersResult.Errors;
        }

        var passenger = passengersResult.Value.FirstOrDefault(passenger => passenger.Id == id);
        return passenger is null
            ? Error.NotFound("Passengers.NotFound", "Passenger was not found.")
            : passenger;
    }

    public async Task<Result<Created>> AddAsync(Passenger passenger)
    {
        var passengersResult = await GetAllAsync();
        if (passengersResult.IsError)
        {
            return passengersResult.Errors;
        }

        var passengers = passengersResult.Value.ToList();
        passengers.Add(passenger);
        var saveResult = await storage.SaveAsync(TransactionFile.Passengers, passengers);
        return saveResult.IsError ? saveResult.Errors : Result.Created;
    }

    public async Task<Result<Updated>> UpdateAsync(Passenger passenger)
    {
        var passengersResult = await GetAllAsync();
        if (passengersResult.IsError)
        {
            return passengersResult.Errors;
        }

        var passengers = passengersResult.Value.ToList();
        var index = passengers.FindIndex(savedPassenger => savedPassenger.Id == passenger.Id);
        
        if (index < 0)
        {
            return Error.NotFound("Passengers.NotFound", "Passenger was not found.");
        }

        passengers[index] = passenger;
        var saveResult = await storage.SaveAsync(TransactionFile.Passengers, passengers);
        return saveResult.IsError ? saveResult.Errors : Result.Updated;
    }
}