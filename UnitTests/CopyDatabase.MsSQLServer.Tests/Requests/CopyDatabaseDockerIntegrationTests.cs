using CopyDatabase.MsSQLServer.Requests.SqlServer;

using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Tests.Requests;

public sealed class CopyDatabaseDockerIntegrationTests
{
    private const string Password = "CopyDb_Test_12345!";
    private const string SourceDatabaseName = "CopyDatabaseSourceTest";
    private const string DestinationDatabaseName = "CopyDatabaseRenamedTest";

    [Fact]
    public async Task CopyDatabaseRequest_CopiesSelectedDatabaseToRenamedDestination()
    {
        if (Environment.GetEnvironmentVariable("COPYDATABASE_DOCKER_SQL_TESTS") != "1") return;

        await ResetSourceFixture();
        await DropDestinationDatabases();

        var progressMessages = new List<DatabaseCopyProgress>();
        var executor = new SqlServerDatabaseCopyExecutor();

        await executor.CopyAsync(new CopyDatabaseRequest
        {
            SourceCredentials = CreateCredentials("127.0.0.1,14333"),
            DestinationCredentials = CreateCredentials("127.0.0.1,14334"),
            DatabaseName = SourceDatabaseName,
            DestinationDatabaseName = DestinationDatabaseName,
            CopySchema = true,
            CopyData = true,
            DropDestinationDatabase = true,
            Progress = new Progress<DatabaseCopyProgress>(progressMessages.Add)
        }, TestContext.Current.CancellationToken);

        await using var connection = new SqlConnection(BuildConnection("127.0.0.1,14334", DestinationDatabaseName));
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, await Scalar<int>(connection, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CopyDatabaseUiTable'"));
        Assert.Equal(1, await Scalar<int>(connection, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CopyDatabaseUiView'"));
        Assert.Equal(2, await Scalar<int>(connection, "SELECT COUNT(*) FROM dbo.CopyDatabaseUiTable"));
        Assert.Contains(progressMessages, oo => oo.ObjectType == "Table" && oo.ObjectName == "dbo.CopyDatabaseUiTable" && oo.Message.Contains("rows copied"));
    }

    [Fact]
    public async Task CopyDatabaseRequest_ReusesExistingDestinationTablesWhenNotDroppingDatabase()
    {
        if (Environment.GetEnvironmentVariable("COPYDATABASE_DOCKER_SQL_TESTS") != "1") return;

        await ResetSourceFixture();
        await ResetExistingDestinationFixture();

        var progressMessages = new List<DatabaseCopyProgress>();
        var executor = new SqlServerDatabaseCopyExecutor();

        await executor.CopyAsync(new CopyDatabaseRequest
        {
            SourceCredentials = CreateCredentials("127.0.0.1,14333"),
            DestinationCredentials = CreateCredentials("127.0.0.1,14334"),
            DatabaseName = SourceDatabaseName,
            DestinationDatabaseName = DestinationDatabaseName,
            CopySchema = true,
            CopyData = true,
            DropDestinationDatabase = false,
            Progress = new Progress<DatabaseCopyProgress>(progressMessages.Add)
        }, TestContext.Current.CancellationToken);

        await using var connection = new SqlConnection(BuildConnection("127.0.0.1,14334", DestinationDatabaseName));
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, await Scalar<int>(connection, "SELECT COUNT(*) FROM dbo.CopyDatabaseUiTable"));
        Assert.Equal("Alpha,Beta", await Scalar<string>(connection, "SELECT STRING_AGG(Name, ',') WITHIN GROUP (ORDER BY Id) FROM dbo.CopyDatabaseUiTable"));
        Assert.Equal(1, await Scalar<int>(connection, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CopyDatabaseUiView'"));
        Assert.Contains(progressMessages, oo => oo.ObjectType == "Table" && oo.ObjectName == "dbo.CopyDatabaseUiTable" && oo.Message.Contains("rows copied"));
    }

    private static DatabaseServerTestCredentials CreateCredentials(string dataSource)
    {
        return new DatabaseServerTestCredentials
        {
            DataSource = dataSource,
            UserName = "sa",
            Password = Password.ToSecureString()
        };
    }

    private static async Task ResetSourceFixture()
    {
        await using var connection = new SqlConnection(BuildConnection("127.0.0.1,14333"));
        await connection.OpenAsync();

        await Execute(connection, $"IF DB_ID('{SourceDatabaseName}') IS NULL CREATE DATABASE {SourceDatabaseName};");

        await using var sourceConnection = new SqlConnection(BuildConnection("127.0.0.1,14333", SourceDatabaseName));
        await sourceConnection.OpenAsync();

        await Execute(sourceConnection, """
            IF OBJECT_ID('dbo.CopyDatabaseUiView','V') IS NOT NULL DROP VIEW dbo.CopyDatabaseUiView;
            IF OBJECT_ID('dbo.CopyDatabaseUiTable','U') IS NOT NULL DROP TABLE dbo.CopyDatabaseUiTable;
            CREATE TABLE dbo.CopyDatabaseUiTable (Id int NOT NULL PRIMARY KEY, Name nvarchar(40) NOT NULL);
            INSERT INTO dbo.CopyDatabaseUiTable (Id, Name) VALUES (1, N'Alpha'), (2, N'Beta');
            """);
        await Execute(sourceConnection, "EXEC('CREATE VIEW dbo.CopyDatabaseUiView AS SELECT Id, Name FROM dbo.CopyDatabaseUiTable');");
    }

    private static async Task DropDestinationDatabases()
    {
        await using var connection = new SqlConnection(BuildConnection("127.0.0.1,14334"));
        await connection.OpenAsync();
        await Execute(connection, $"""
            IF DB_ID('{DestinationDatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE {DestinationDatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {DestinationDatabaseName};
            END;
            IF DB_ID('{SourceDatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE {SourceDatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {SourceDatabaseName};
            END;
            """);
    }

    private static async Task ResetExistingDestinationFixture()
    {
        await DropDestinationDatabases();

        await using var connection = new SqlConnection(BuildConnection("127.0.0.1,14334"));
        await connection.OpenAsync();
        await Execute(connection, $"CREATE DATABASE {DestinationDatabaseName};");

        await using var destinationConnection = new SqlConnection(BuildConnection("127.0.0.1,14334", DestinationDatabaseName));
        await destinationConnection.OpenAsync();
        await Execute(destinationConnection, """
            CREATE TABLE dbo.CopyDatabaseUiTable (Id int NOT NULL PRIMARY KEY, Name nvarchar(40) NOT NULL);
            INSERT INTO dbo.CopyDatabaseUiTable (Id, Name) VALUES (99, N'Old row');
            """);
    }

    private static string BuildConnection(string dataSource, string databaseName = "")
    {
        var builder = new StringBuilder($"Data Source={dataSource};Integrated Security=False;Encrypt=False;User ID=sa;Password={Password};");
        if (!string.IsNullOrWhiteSpace(databaseName)) builder.Append($"Initial Catalog={databaseName};");
        return builder.ToString();
    }

    private static async Task Execute(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 9000;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> Scalar<T>(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        object? value = await command.ExecuteScalarAsync();
        return Assert.IsType<T>(value);
    }
}
