using CopyDatabase.Core.Requests;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerDataCopier
{
    public async Task CopyAsync(
        CopyDatabaseRequest request,
        string sourceConnectionString,
        SqlConnection destinationConnection,
        SqlTransaction transaction,
        IReadOnlyCollection<SqlServerSchemaObject> tables,
        CancellationToken cancellationToken)
    {
        int counter = 1;
        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SqlServerCopyProgress.ReportTable(request, table, $"Copying data {counter} of {tables.Count}");

            await SqlServerDatabaseCatalog.ExecuteNonQueryAsync(destinationConnection, transaction, BuildDeleteAllSql(table), cancellationToken);

            await using var sourceConnection = new SqlConnection(sourceConnectionString);
            await sourceConnection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(BuildSelectAllSql(table), sourceConnection);
            command.CommandTimeout = 9000;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            using var bulkCopy = new SqlBulkCopy(destinationConnection, SqlBulkCopyOptions.Default, transaction)
            {
                DestinationTableName = table.QualifiedName,
                BulkCopyTimeout = 9000,
                EnableStreaming = true,
                NotifyAfter = 1
            };

            bulkCopy.SqlRowsCopied += (_, args) =>
            {
                SqlServerCopyProgress.ReportTable(request, table, $"Copying data {counter} of {tables.Count} - {args.RowsCopied} rows copied");
            };

            await bulkCopy.WriteToServerAsync(reader, cancellationToken);
            counter++;
        }
    }

    [SuppressMessage(
        "Security",
        "S2077:SQL queries should not be vulnerable to injection attacks",
        Justification = "Schema and table names are loaded from SQL Server catalog metadata and escaped with QUOTENAME-equivalent brackets before being composed.")]
    internal static string BuildDeleteAllSql(SqlServerSchemaObject table)
    {
        return $"DELETE FROM {table.QualifiedName};";
    }

    [SuppressMessage(
        "Security",
        "S2077:SQL queries should not be vulnerable to injection attacks",
        Justification = "Schema and table names are loaded from SQL Server catalog metadata and escaped with QUOTENAME-equivalent brackets before being composed.")]
    internal static string BuildSelectAllSql(SqlServerSchemaObject table)
    {
        return $"SELECT * FROM {table.QualifiedName};";
    }
}

