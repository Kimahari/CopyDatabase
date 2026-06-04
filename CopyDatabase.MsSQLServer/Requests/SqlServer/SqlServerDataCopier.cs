using CopyDatabase.Core.Requests;

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

            await SqlServerDatabaseCatalog.ExecuteNonQueryAsync(destinationConnection, transaction, $"DELETE FROM {table.QualifiedName};", cancellationToken);

            await using var sourceConnection = new SqlConnection(sourceConnectionString);
            await sourceConnection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand($"SELECT * FROM {table.QualifiedName};", sourceConnection);
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
}

