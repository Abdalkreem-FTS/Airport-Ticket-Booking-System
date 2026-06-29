namespace ATBS.Storage;

public static class ProjectPathResolver
{
    private const string ProjectFileName = "ATBS.csproj";

    public static string GetProjectDirectory()
    {
        return FindProjectDirectory(Directory.GetCurrentDirectory())
            ?? FindProjectDirectory(AppContext.BaseDirectory)
            ?? Directory.GetCurrentDirectory();
    }

    private static string? FindProjectDirectory(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
