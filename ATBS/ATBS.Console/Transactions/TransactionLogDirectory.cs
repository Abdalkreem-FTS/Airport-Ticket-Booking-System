using ATBS.Console.Storage;

namespace ATBS.Console.Transactions;

public sealed class TransactionLogDirectory(FilePaths filePaths)
{
    public string DirectoryPath { get; } = Path.Combine(filePaths.DataDirectory, "TransactionLogs");
}