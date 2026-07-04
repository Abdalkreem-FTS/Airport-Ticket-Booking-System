using ATBS.Console.Transactions;

namespace ATBS.Tests;

public sealed class RecoveryTests
{
    [Fact]
    public async Task Interrupted_Commit_In_Committing_State_Is_Rolled_Forward()
    {
        using var harness = new TransactionHarness();
        var logDirectory = harness.LogDirectory.DirectoryPath;

        var finalPath = Path.Combine(harness.DataDirectory, "bookings.json");
        await File.WriteAllTextAsync(finalPath, "OLD");

        var id = Guid.NewGuid();
        var tempPath = finalPath + $".{id:N}.temporary";
        await File.WriteAllTextAsync(tempPath, "NEW");

        var log = TransactionLog.Create(logDirectory, id);
        await log.AddEntry(new TransactionLogEntry { TemporaryPath = tempPath, FinalPath = finalPath });
        await log.MarkCommitting();

        await TransactionRecovery.RecoverAll(logDirectory);

        Assert.Equal("NEW", await File.ReadAllTextAsync(finalPath)); 
        Assert.False(File.Exists(tempPath));
        Assert.Empty(Directory.GetFiles(logDirectory, "*.log"));
    }

    [Fact]
    public async Task Interrupted_Transaction_In_Pending_State_Is_Rolled_Back()
    {
        using var harness = new TransactionHarness();
        var logDirectory = harness.LogDirectory.DirectoryPath;

        var finalPath = Path.Combine(harness.DataDirectory, "bookings.json");
        await File.WriteAllTextAsync(finalPath, "OLD");

        var id = Guid.NewGuid();
        var tempPath = finalPath + $".{id:N}.temporary";
        await File.WriteAllTextAsync(tempPath, "NEW");

        var log = TransactionLog.Create(logDirectory, id);
        await log.AddEntry(new TransactionLogEntry { TemporaryPath = tempPath, FinalPath = finalPath });

        await TransactionRecovery.RecoverAll(logDirectory);

        Assert.Equal("OLD", await File.ReadAllTextAsync(finalPath));
        Assert.False(File.Exists(tempPath));
        Assert.Empty(Directory.GetFiles(logDirectory, "*.log"));
    }

    [Fact]
    public async Task Committed_State_Only_Cleans_Up_And_Leaves_The_Final_File_In_Place()
    {
        using var harness = new TransactionHarness();
        var logDirectory = harness.LogDirectory.DirectoryPath;

        // Crash happened after the commit finished but before cleanup: the final file already
        // holds the new content, and a leftover temp file plus a Committed log remain.
        var finalPath = Path.Combine(harness.DataDirectory, "bookings.json");
        await File.WriteAllTextAsync(finalPath, "NEW");

        var id = Guid.NewGuid();
        var tempPath = finalPath + $".{id:N}.temporary";
        await File.WriteAllTextAsync(tempPath, "NEW");

        var log = TransactionLog.Create(logDirectory, id);
        await log.AddEntry(new TransactionLogEntry { TemporaryPath = tempPath, FinalPath = finalPath });
        await log.MarkCommitting();
        await log.MarkCommitted();

        await TransactionRecovery.RecoverAll(logDirectory);

        Assert.Equal("NEW", await File.ReadAllTextAsync(finalPath));
        Assert.False(File.Exists(tempPath));
        Assert.Empty(Directory.GetFiles(logDirectory, "*.log"));
    }

    [Fact]
    public async Task RollingBack_State_Is_Rolled_Back_And_The_Final_File_Is_Untouched()
    {
        using var harness = new TransactionHarness();
        var logDirectory = harness.LogDirectory.DirectoryPath;

        var finalPath = Path.Combine(harness.DataDirectory, "bookings.json");
        await File.WriteAllTextAsync(finalPath, "OLD");

        var id = Guid.NewGuid();
        var tempPath = finalPath + $".{id:N}.temporary";
        await File.WriteAllTextAsync(tempPath, "NEW");

        var log = TransactionLog.Create(logDirectory, id);
        await log.AddEntry(new TransactionLogEntry { TemporaryPath = tempPath, FinalPath = finalPath });
        log.MarkRollingBackSync();

        await TransactionRecovery.RecoverAll(logDirectory);

        Assert.Equal("OLD", await File.ReadAllTextAsync(finalPath));
        Assert.False(File.Exists(tempPath));
        Assert.Empty(Directory.GetFiles(logDirectory, "*.log"));
    }

    [Fact]
    public async Task Unreadable_Log_Is_Removed_And_Its_Orphaned_Temp_Files_Are_Cleaned_Up()
    {
        using var harness = new TransactionHarness();
        var logDirectory = harness.LogDirectory.DirectoryPath;
        Directory.CreateDirectory(logDirectory);

        // The log file itself was torn mid-write and no longer parses as JSON, but its name is a
        // valid transaction id, so recovery can still find and delete that transaction's orphans.
        var id = Guid.NewGuid();
        var corruptLog = Path.Combine(logDirectory, $"{id}.log");
        await File.WriteAllTextAsync(corruptLog, "{ this is not valid json");

        var orphanTemp = Path.Combine(harness.DataDirectory, $"bookings.json.{id:N}.temporary");
        await File.WriteAllTextAsync(orphanTemp, "NEW");

        await TransactionRecovery.RecoverAll(logDirectory);

        Assert.False(File.Exists(corruptLog));
        Assert.False(File.Exists(orphanTemp));
        Assert.Empty(Directory.GetFiles(logDirectory, "*.log"));
    }
}