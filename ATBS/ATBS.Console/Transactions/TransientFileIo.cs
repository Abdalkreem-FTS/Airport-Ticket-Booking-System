namespace ATBS.Console.Transactions;

/// <summary>
/// Bounded-retry helpers for the physical file operations at the storage boundary.
///
/// Under SNAPSHOT isolation, writers take no locks, so a transaction's atomic temp→final replace can
/// momentarily overlap another transaction reading the same file from disk (for example, seeding a
/// version-chain base). On Windows an open read handle blocks a <c>File.Replace</c>, and a replacement in
/// progress blocks an open — both surface as transient <see cref="IOException"/> sharing violations.
/// These conflicts are short-lived: retrying a few times with a small backoff lets the other
/// operation finish, then succeeds. This does not change isolation semantics; it only makes the
/// unavoidable physical contention robust.
/// </summary>
internal static class TransientFileIo
{
    private const int MaxAttempts = 25;
    private const int BaseDelayMs = 4;
    private const int MaxDelayMs = 60;

    public static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await File.ReadAllTextAsync(path, cancellationToken);
            }
            catch (IOException) when (attempt < MaxAttempts && File.Exists(path))
            {
                await Task.Delay(Delay(attempt), cancellationToken);
            }
        }
    }

    public static void Run(Action fileOperation)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                fileOperation();
                return;
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(Delay(attempt));
            }
        }
    }

    private static int Delay(int attempt) => Math.Min(MaxDelayMs, BaseDelayMs * attempt) + Random.Shared.Next(0, 4);
}