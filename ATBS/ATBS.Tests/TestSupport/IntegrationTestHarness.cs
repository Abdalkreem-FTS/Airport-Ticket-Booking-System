using ATBS.Console;
using ATBS.Console.Abstractions;
using ATBS.Console.DTOs;
using ATBS.Console.Models;
using ATBS.Console.Models.Enums;
using ATBS.Console.Results;
using ATBS.Console.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ATBS.Tests.TestSupport;

/// <summary>
/// Boots the application's real object graph — the exact same <see cref="AppServiceRegistration.AddAppServices"/>
/// composition that <c>Program.cs</c> uses — but points <see cref="FilePaths"/> at a throwaway temp directory.
/// Nothing is mocked: services run through the real transaction factory, lock manager, write-ahead log, and JSON
/// file storage. That is the point of these tests — to prove the pieces work <i>together</i>, including the parts
/// (locking, staging, commit/rollback, on-disk persistence) that the unit tests deliberately stub out.
///
/// Each harness owns an isolated data directory and service provider, so tests stay independent and parallel-safe.
/// Disposing tears down the provider and deletes the directory.
/// </summary>
internal sealed class IntegrationTestHarness : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public IntegrationTestHarness()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "atbs_integration_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DataDirectory);

        var services = new ServiceCollection();
        services.AddAppServices();

        // Registered last, so it wins over the production FilePaths registration. Every path the app touches
        // (flights.json, bookings.json, passengers.json, TransactionLogs/) is now rooted under our temp directory.
        services.AddSingleton(new FilePaths(DataDirectory));

        _provider = services.BuildServiceProvider();
    }

    public string DataDirectory { get; }

    public IFlightService FlightService => Get<IFlightService>();
    public IBookingService BookingService => Get<IBookingService>();
    public IManagerBookingService ManagerBookingService => Get<IManagerBookingService>();
    public IFlightImportService FlightImportService => Get<IFlightImportService>();
    public IFlightRepository FlightRepository => Get<IFlightRepository>();
    public IBookingRepository BookingRepository => Get<IBookingRepository>();
    public IPassengerRepository PassengerRepository => Get<IPassengerRepository>();
    public IFileTransactionFactory TransactionFactory => Get<IFileTransactionFactory>();

    /// <summary>Write-ahead log files still on disk; empty once every transaction has committed or rolled back.</summary>
    public IReadOnlyList<string> PendingTransactionLogFiles
    {
        get
        {
            var directory = Path.Combine(DataDirectory, "TransactionLogs");
            return Directory.Exists(directory)
                ? Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                : [];
        }
    }

    public T Get<T>() where T : notnull => _provider.GetRequiredService<T>();

    /// <summary>Persists a passenger straight to disk (no ambient transaction) and returns it for use in a test.</summary>
    public async Task<Passenger> SeedPassengerAsync(Passenger? passenger = null)
    {
        passenger ??= Builders.NewPassenger();
        Ensure(await PassengerRepository.AddAsync(passenger), "seed passenger");
        return passenger;
    }

    public async Task<Flight> SeedFlightAsync(Flight flight)
    {
        Ensure(await FlightRepository.AddAsync(flight), "seed flight");
        return flight;
    }

    /// <summary>Books through the real <see cref="IBookingService"/> — the transaction factory does the rest.</summary>
    public Task<Result<Booking>> BookAsync(Passenger passenger, Flight flight, FlightClass flightClass = FlightClass.Economy) =>
        BookingService.BookFlightAsync(new CreateBookingRequest
        {
            PassengerId = passenger.Id,
            FlightId = flight.Id,
            Class = flightClass
        });

    /// <summary>Re-reads a flight from storage so a test can assert what was actually persisted.</summary>
    public Task<Flight> ReloadFlightAsync(Flight flight) => ReloadFlightAsync(flight.Id);

    public async Task<Flight> ReloadFlightAsync(Guid flightId)
    {
        var result = await FlightRepository.GetByIdAsync(flightId);
        Ensure(result, "reload flight");
        return result.Value;
    }

    public async Task<IReadOnlyList<Booking>> ReloadBookingsAsync()
    {
        var result = await BookingRepository.GetAllAsync();
        Ensure(result, "reload bookings");
        return result.Value;
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();

        try
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }

    private static void Ensure<T>(Result<T> result, string action)
    {
        if (result.IsError)
        {
            throw new InvalidOperationException($"Integration harness failed to {action}: {result.TopError.Description}");
        }
    }
}
