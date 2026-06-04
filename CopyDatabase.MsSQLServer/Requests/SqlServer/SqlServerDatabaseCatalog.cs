using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerDatabaseCatalog
{
    public async Task RecreateDatabaseAsync(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        string name = SqlServerSql.QuoteName(databaseName);
        string sql = $@"
IF DB_ID(N'{SqlServerSql.EscapeSqlLiteral(databaseName)}') IS NOT NULL
BEGIN
    ALTER DATABASE {name} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE {name};
END;
CREATE DATABASE {name};";

        await ExecuteNonQueryAsync(connection, null, sql, cancellationToken);
    }

    public async Task CreateDatabaseIfMissingAsync(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        string sql = $@"
IF DB_ID(N'{SqlServerSql.EscapeSqlLiteral(databaseName)}') IS NULL
BEGIN
    CREATE DATABASE {SqlServerSql.QuoteName(databaseName)};
END";
        await ExecuteNonQueryAsync(connection, null, sql, cancellationToken);
    }

    public async Task<List<SqlServerSchemaObject>> LoadTablesAsync(string connectionString, CancellationToken cancellationToken)
    {
        var result = new List<SqlServerSchemaObject>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(SqlServerSql.Tables, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SqlServerSchemaObject(reader.GetString(0), reader.GetString(1), ""));
        }

        return result;
    }

    public async Task<List<SqlServerSchemaObject>> LoadScriptedObjectsAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        var result = new List<SqlServerSchemaObject>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SqlServerSchemaObject(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return result;
    }

    public async Task EnsureSchemasCreatedAsync(SqlConnection connection, SqlTransaction transaction, IEnumerable<string> schemas, CancellationToken cancellationToken)
    {
        foreach (string schema in schemas.Distinct(StringComparer.OrdinalIgnoreCase).Where(oo => !oo.Equals("dbo", StringComparison.OrdinalIgnoreCase)))
        {
            string sql = $"IF SCHEMA_ID(N'{SqlServerSql.EscapeSqlLiteral(schema)}') IS NULL EXEC(N'CREATE SCHEMA {SqlServerSql.QuoteName(schema)}');";
            await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken);
        }
    }

    public Task<bool> TableExistsAsync(SqlConnection connection, SqlTransaction transaction, SqlServerSchemaObject table, CancellationToken cancellationToken)
    {
        return ObjectExistsAsync(connection, transaction, table, cancellationToken, "U");
    }

    public async Task<bool> ObjectExistsAsync(SqlConnection connection, SqlTransaction transaction, SqlServerSchemaObject item, CancellationToken cancellationToken, string? objectType = null)
    {
        const string sql = @"
SELECT CAST(CASE WHEN EXISTS (
    SELECT 1
    FROM sys.objects o
    INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = @schema
      AND o.name = @name
      AND (@type IS NULL OR o.type = @type)
) THEN 1 ELSE 0 END AS bit);";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@schema", item.Schema);
        command.Parameters.AddWithValue("@name", item.Name);
        command.Parameters.AddWithValue("@type", (object?)objectType ?? DBNull.Value);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    public static async Task ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction? transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        command.CommandTimeout = 9000;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

