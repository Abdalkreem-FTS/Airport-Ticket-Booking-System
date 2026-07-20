namespace ATBS.Console.Transactions.Abstractions;

/// <summary>
/// Tracks in-flight (uncommitted) staged writes so that READ UNCOMMITTED transactions can observe
/// dirty data from other active transactions.
/// </summary>
public interface IStagedStore
{
    void Register(string finalPath, string temporaryPath);

    void Unregister(string finalPath, string temporaryPath);

    /// <summary>Returns the most recently staged content for <paramref name="finalPath"/>, or null if none is staged.</summary>
    string? ReadAny(string finalPath);
}