using ATBS.Console.Transactions.Abstractions;
using ATBS.Console.Transactions.ConcurrencyControl;

namespace ATBS.Console.Transactions.Management;

/// <summary>
/// Multi-version store backing SNAPSHOT isolation. Each resource keeps a version chain of
/// (sequence, content) entries — sequence 0 is the base (the content before this process first
/// committed the resource). A snapshot reads the entry with the greatest sequence not exceeding its
/// beginning sequence, so it always sees a consistent as-of-begin view from memory, never from a file
/// that a concurrent commit may be mid-way through replacing. Versions no active snapshot can see
/// are garbage-collected. See <see cref="SnapshotStrategy"/>.
/// </summary>
public sealed class VersionStore : IVersionStore
{
    private readonly Dictionary<string, List<(long Sequence, string Content)>> _versions = new();
    private readonly List<long> _activeSnapshots = [];
    private long _globalSequence;

    private readonly Lock _guard = new();

    public long BeginSnapshot()
    {
        lock (_guard)
        {
            _activeSnapshots.Add(_globalSequence);
            
            return _globalSequence;
        }
    }

    public void EndSnapshot(long snapshotSequence)
    {
        lock (_guard)
        {
            _activeSnapshots.Remove(snapshotSequence);
            
            CollectGarbage();
        }
    }

    public async Task<string> GetSnapshotContentAsync(string path, long snapshotSequence, CancellationToken cancellationToken = default)
    {
        path = Path.GetFullPath(path);

        lock (_guard)
        {
            if (_versions.TryGetValue(path, out var existing))
            {
                return Resolve(existing, snapshotSequence);
            }
        }
        
        var baseContent = File.Exists(path)
            ? await TransientFileIo.ReadAllTextAsync(path, cancellationToken)
            : string.Empty;

        lock (_guard)
        {
            var chain = EnsureBase(path, baseContent);

            return Resolve(chain, snapshotSequence);
        }
    }

    private static string Resolve(List<(long Sequence, string Content)> chain, long snapshotSequence)
    {
        var content = chain[0].Content;
        foreach (var (sequence, versionContent) in chain)
        {
            if (sequence > snapshotSequence)
            {
                break;
            }

            content = versionContent;
        }

        return content;
    }

    public bool TryCommitSnapshot(IReadOnlyDictionary<string, ResourceChange> changes, long snapshotSequence)
    {
        lock (_guard)
        {
            foreach (var (rawPath, change) in changes)
            {
                var chain = EnsureBase(Path.GetFullPath(rawPath), change.PreImage);
                if (chain[^1].Sequence > snapshotSequence)
                {
                    return false;
                }
            }

            Apply(changes);
            
            return true;
        }
    }

    public void CommitLocked(IReadOnlyDictionary<string, ResourceChange> changes)
    {
        lock (_guard)
        {
            Apply(changes);
        }
    }

    private void Apply(IReadOnlyDictionary<string, ResourceChange> changes)
    {
        var newSequence = ++_globalSequence;

        foreach (var (rawPath, change) in changes)
        {
            var path = Path.GetFullPath(rawPath);
            var chain = EnsureBase(path, change.PreImage);
            chain.Add((newSequence, change.PostImage));
        }

        CollectGarbage();
    }

    private List<(long Sequence, string Content)> EnsureBase(string path, string baseContent)
    {
        if (!_versions.TryGetValue(path, out var chain))
        {
            _versions[path] = chain = [(0, baseContent)];
        }

        return chain;
    }

    private void CollectGarbage()
    {
        foreach (var chain in _versions.Values)
        {
            if (chain.Count == 1)
            {
                continue;
            }

            var keep = new HashSet<long>
            {
                chain[^1].Sequence
            };

            foreach (var snapshot in _activeSnapshots)
            {
                var visible = chain[0].Sequence;
                foreach (var (sequence, _) in chain)
                {
                    if (sequence > snapshot)
                    {
                        break;
                    }

                    visible = sequence;
                }

                keep.Add(visible);
            }

            chain.RemoveAll(entry => !keep.Contains(entry.Sequence));
        }
    }
}