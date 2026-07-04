using ATBS.Console.Storage;
using ATBS.Console.Transactions.Enums;

namespace ATBS.Console.Transactions;

public sealed class TransactionFileCatalog(FilePaths filePaths)
{
    public string GetPath(TransactionFile file)
    {
        var path = file switch
        {
            TransactionFile.Flights => filePaths.FlightsPath,
            TransactionFile.Bookings => filePaths.BookingsPath,
            TransactionFile.Passengers => filePaths.PassengersPath,
            _ => throw new ArgumentOutOfRangeException(nameof(file), file, "Unsupported transaction file.")
        };

        return Path.GetFullPath(path);
    }
}