using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerObjectScripter
{
    public async Task<string> ScriptTableAsync(string sourceConnectionString, SqlServerSchemaObject table, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(sourceConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(SqlServerSql.CreateTableScript.Replace("{Schema}.{Name}", $"{table.Schema}.{table.Name}"), connection);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        string? script = result as string;

        if (string.IsNullOrWhiteSpace(script))
        {
            throw new InvalidOperationException($"Could not script table {table.QualifiedName}.");
        }

        return script.Replace("#TABLENAME", SqlServerSql.QuoteName(table.Name)).Replace("IDENTITY(*,1)", "IDENTITY(1,1)");
    }
}

