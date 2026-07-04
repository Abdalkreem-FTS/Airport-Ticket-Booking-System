using System.Text.Json;
using ATBS.Console.Abstractions;
using ATBS.Console.Services;
using ATBS.Console.Storage;
using ATBS.Console.Storage.Repositories;
using ATBS.Console.Transactions;
using ATBS.Console.Transactions.ConcurrencyControl;
using ATBS.Console.Transactions.Management;

namespace ATBS.Tests;

/// <summary>
/// Builds a fully wired, self-contained transaction stack over a throwaway temporary directory. Every
/// harness has its own lock manager / version store / staged store (no global state), so tests are
/// isolated and can run in parallel.
/// </summary>
public sealed class TransactionHarness : IDisposable
{
    public TransactionHarness(int lockTimeoutMs = 10000)
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "atbs_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DataDirectory);

        FilePaths = new FilePaths(DataDirectory);
        Catalog = new TransactionFileCatalog(FilePaths);
        Context = new FileTransactionContext();
        LogDirectory = new TransactionLogDirectory(FilePaths);
        LockManager = new LockManager(lockTimeoutMs);
        VersionStore = new VersionStore();
        StagedStore = new StagedStore();

        Factory = new FileTransactionFactory(
            Catalog, Context, LogDirectory, new ConcurrencyControlStrategyFactory(),
            LockManager, VersionStore, StagedStore);

        Storage = new TransactionalJsonFileStorage(Catalog, Context);
        Flights = new FlightRepository(Storage);
        Bookings = new BookingRepository(Storage);
        Passengers = new PassengerRepository(Storage);
        BookingService = new BookingService(Bookings, Flights, Passengers, Factory);
    }

    public string DataDirectory { get; }
    public FilePaths FilePaths { get; }
    public TransactionFileCatalog Catalog { get; }
    public FileTransactionContext Context { get; }
    public TransactionLogDirectory LogDirectory { get; }
    public LockManager LockManager { get; }
    public VersionStore VersionStore { get; }
    public StagedStore StagedStore { get; }
    public FileTransactionFactory Factory { get; }
    public ITransactionalFileStorage Storage { get; }
    public FlightRepository Flights { get; }
    public BookingRepository Bookings { get; }
    public PassengerRepository Passengers { get; }
    public BookingService BookingService { get; }

    public void SeedJson<T>(string fileName, IEnumerable<T> items) =>
        File.WriteAllText(Path.Combine(DataDirectory, fileName), JsonSerializer.Serialize(items));

    public void Dispose()
    {
        try
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}