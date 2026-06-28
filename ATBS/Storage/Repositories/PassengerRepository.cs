using ATBS.Abstractions;
using ATBS.Models;

namespace ATBS.Storage.Repositories;

public sealed class PassengerRepository(IFileStorage storage, FilePaths filePaths) : IPassengerRepository
{
    public IReadOnlyList<Passenger> GetAll() => storage.Load<Passenger>(filePaths.PassengersPath);

    public Passenger? GetById(Guid id) => GetAll().FirstOrDefault(passenger => passenger.Id == id);

    public void Add(Passenger passenger)
    {
        var passengers = GetAll().ToList();
        passengers.Add(passenger);
        storage.Save(filePaths.PassengersPath, passengers);
    }

    public void Update(Passenger passenger)
    {
        var passengers = GetAll().ToList();
        var index = passengers.FindIndex(savedPassenger => savedPassenger.Id == passenger.Id);
        
        if (index < 0)
        {
            throw new InvalidOperationException("Passenger was not found.");
        }

        passengers[index] = passenger;
        storage.Save(filePaths.PassengersPath, passengers);
    }
}
