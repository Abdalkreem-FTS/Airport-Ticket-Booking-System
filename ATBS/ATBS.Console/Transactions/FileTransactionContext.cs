namespace ATBS.Console.Transactions;

public sealed class FileTransactionContext
{
    private readonly AsyncLocal<FileTransaction?> _current = new(); // same idea that DbContext in EF Core uses

    public FileTransaction? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}