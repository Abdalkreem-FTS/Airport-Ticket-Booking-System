using ATBS.Console.Transactions.Enums;
using ATBS.Console.Transactions.Exceptions;

namespace ATBS.Console.Transactions;

public static class TransactionRecovery
{
    public static async Task RecoverAll(string transactionLogDirectory)
    {
        if (!Directory.Exists(transactionLogDirectory))
        {
            return;
        }

        foreach (var logFile in Directory.GetFiles(transactionLogDirectory, "*.log"))
        {
            var log = await TransactionLog.Load(logFile);

            if (log is null)
            {
                var stem = Path.GetFileNameWithoutExtension(logFile);
                if (Guid.TryParse(stem, out var orphanId))
                {
                    BestEffortCleanupOrphans(transactionLogDirectory, orphanId);
                }

                File.Delete(logFile);
                
                continue;
            }

            switch (log.Status)
            {
                case TransactionLogStatus.Pending:
                    log.Rollback();
                    break;
                
                case TransactionLogStatus.Committing:
                    log.ReplayCommit();
                    await log.MarkCommitted();
                    log.Cleanup();
                    break;

                case TransactionLogStatus.Committed:
                    log.Cleanup();
                    break;
                
                case TransactionLogStatus.RollingBack:
                    log.Rollback();
                    break;
                
                case TransactionLogStatus.RolledBack:
                    log.Cleanup();
                    break;

                default:
                    throw new TransactionException(
                        "Unsupported transaction log status: " + log.Status);
            }

            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        }
    }
    
    private static void BestEffortCleanupOrphans(string logDirectory, Guid transactionId)
    {
        var txId = transactionId.ToString("N");

        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            logDirectory,
            Path.GetDirectoryName(logDirectory) ?? logDirectory
        };

        foreach (var directory in directories.Where(Directory.Exists))
        {
            foreach (var file in Directory.GetFiles(directory, $"*.{txId}.temporary"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    System.Console.Error.WriteLine(
                        $"[Recovery] Could not delete orphaned file '{file}' " +
                        $"(transaction {txId[..8]}). Remove manually.");
                }
            }
        }
    }
}