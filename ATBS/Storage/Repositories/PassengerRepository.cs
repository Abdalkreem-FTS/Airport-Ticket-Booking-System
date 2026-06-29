using ATBS.Abstractions;
using ATBS.Models;

namespace ATBS.Storage.Repositories;

public sealed class PassengerRepository(IFileStorage storage, FilePaths filePaths) : IPassengerRepository
{
    public async Task<IEnumerable<Passenger>> GetAllAsync() => await storage.LoadAsync<Passenger>(filePaths.PassengersPath);

    public async Task<Passenger?> GetByIdAsync(Guid id) => (await GetAllAsync()).FirstOrDefault(passenger => passenger.Id == id);

    public async Task AddAsync(Passenger passenger)
    {
        var passengers = (await GetAllAsync()).ToList();
        passengers.Add(passenger);
        await storage.SaveAsync(filePaths.PassengersPath, passengers);
    }

    public async Task UpdateAsync(Passenger passenger)
    {
        var passengers = (await GetAllAsync()).ToList();
        var index = passengers.FindIndex(savedPassenger => savedPassenger.Id == passenger.Id);
        
        if (index < 0)
        {
            throw new InvalidOperationException("Passenger was not found.");
        }

        passengers[index] = passenger;
        await storage.SaveAsync(filePaths.PassengersPath, passengers);
    }
}
