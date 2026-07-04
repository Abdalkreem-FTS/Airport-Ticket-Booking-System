using ATBS.Console.Transactions.Abstractions;

namespace ATBS.Console.Transactions.Management;

/// <summary>
/// Thread-safe registry of in-flight staged writes (final path → the temporary files staged for it),
/// enabling READ UNCOMMITTED transactions to observe dirty data from other active transactions.
/// </summary>
public sealed class StagedStore : IStagedStore
{
    private readonly Dictionary<string, List<string>> _registry = [];
    private readonly Lock _guard = new();

    public void Register(string finalPath, string temporaryPath)
    {
        finalPath = Path.GetFullPath(finalPath);

        lock (_guard)
        {
            if (!_registry.TryGetValue(finalPath, out var list))
            {
                _registry[finalPath] = list = [];
            }

            list.Add(temporaryPath);
        }
    }

    public void Unregister(string finalPath, string temporaryPath)
    {
        finalPath = Path.GetFullPath(finalPath);

        lock (_guard)
        {
            if (!_registry.TryGetValue(finalPath, out var list))
            {
                return;
            }

            list.Remove(temporaryPath);
            if (list.Count == 0)
            {
                _registry.Remove(finalPath);
            }
        }
    }

    public string? ReadAny(string finalPath)
    {
        finalPath = Path.GetFullPath(finalPath);

        lock (_guard)
        {
            return !_registry.TryGetValue(finalPath, out var list)
                ? null
                : ((IEnumerable<string>)list)
                .Reverse()
                .Where(File.Exists)
                .Select(File.ReadAllText)
                .FirstOrDefault();
        }
    }
}