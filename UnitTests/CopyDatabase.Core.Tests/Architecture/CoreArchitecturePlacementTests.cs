namespace CopyDatabase.Core.Tests.Architecture;

public sealed class CoreArchitecturePlacementTests
{
    [Fact]
    public void ApplicationProject_DoesNotDependOnSqlServerImplementation()
    {
        var root = FindRepositoryRoot();
        var projectFile = Path.Combine(root, "CopyDatabase.Application", "CopyDatabase.Core.csproj");
        var projectXml = File.ReadAllText(projectFile);

        Assert.DoesNotContain("CopyDatabase.MsSQLServer", projectXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.Data.SqlClient", projectXml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplicationProject_DoesNotContainSqlServerAdapterFiles()
    {
        var root = FindRepositoryRoot();
        var applicationPath = Path.Combine(root, "CopyDatabase.Application");
        var sqlServerFiles = Directory
            .EnumerateFiles(applicationPath, "*.cs", SearchOption.AllDirectories)
            .Where(oo => oo.Contains($"{Path.DirectorySeparatorChar}SqlServer{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(sqlServerFiles);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CopyDatabase.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find CopyDatabase.sln from the test output directory.");
    }
}
