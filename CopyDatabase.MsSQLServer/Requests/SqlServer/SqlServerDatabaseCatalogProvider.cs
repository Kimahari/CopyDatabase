using CopyDatabase.Common;
using CopyDatabase.Core.Requests;

using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerDatabaseCatalogProvider : IDatabaseCatalogProvider
{
    private readonly IConnectionStringBuilder connectionStringBuilder;

    public SqlServerDatabaseCatalogProvider(IConnectionStringBuilder connectionStringBuilder)
    {
        this.connectionStringBuilder = connectionStringBuilder;
    }

    public async Task<string[]> GetDatabaseNamesAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken)
    {
        string connectionString = BuildConnection(credentials);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT name FROM sys.databases", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0));
        return result.ToArray();
    }

    public async Task<string[]> GetSchemaObjectNamesAsync(
        IDatabaseServerCredentials credentials,
        string databaseName,
        SchemaObjectType objectType,
        CancellationToken cancellationToken)
    {
        string connectionString = BuildConnection(credentials, databaseName);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = objectType switch
        {
            SchemaObjectType.Table => """
                SELECT TABLE_SCHEMA + '.' + TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME
                """,
            SchemaObjectType.View => """
                SELECT TABLE_SCHEMA + '.' + TABLE_NAME
                FROM INFORMATION_SCHEMA.VIEWS
                ORDER BY TABLE_SCHEMA, TABLE_NAME
                """,
            SchemaObjectType.Routine => """
                SELECT SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME
                FROM INFORMATION_SCHEMA.ROUTINES
                ORDER BY SPECIFIC_SCHEMA, SPECIFIC_NAME
                """,
            _ => throw new NotImplementedException()
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0));
        return result.ToArray();
    }

    private string BuildConnection(IDatabaseServerCredentials credentials, string databaseName = "")
    {
        return connectionStringBuilder.BuildConnection(credentials, databaseName).FromSecureString();
    }
}
