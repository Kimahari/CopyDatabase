using CopyDatabase.Core.Requests;

using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerSchemaCopier
{
    private readonly SqlServerDatabaseCatalog catalog;
    private readonly SqlServerObjectScripter objectScripter;

    public SqlServerSchemaCopier(SqlServerDatabaseCatalog catalog, SqlServerObjectScripter objectScripter)
    {
        this.catalog = catalog;
        this.objectScripter = objectScripter;
    }

    public async Task CopyAsync(
        CopyDatabaseRequest request,
        string sourceConnectionString,
        SqlConnection destinationConnection,
        SqlTransaction transaction,
        IReadOnlyCollection<SqlServerSchemaObject> tables,
        IReadOnlyCollection<SqlServerSchemaObject> foreignKeys,
        IReadOnlyCollection<SqlServerSchemaObject> routines,
        IReadOnlyCollection<SqlServerSchemaObject> views,
        CancellationToken cancellationToken)
    {
        await catalog.EnsureSchemasCreatedAsync(destinationConnection, transaction, tables.Select(oo => oo.Schema), cancellationToken);
        await CopyTableSchemasAsync(request, sourceConnectionString, destinationConnection, transaction, tables, cancellationToken);
        await CopyScriptedObjectsAsync(request, destinationConnection, transaction, foreignKeys, "foreign key", cancellationToken, "F");
        await CopyScriptedObjectsAsync(request, destinationConnection, transaction, routines, "routine", cancellationToken);
        await CopyScriptedObjectsAsync(request, destinationConnection, transaction, views, "view", cancellationToken);
    }

    private async Task CopyTableSchemasAsync(
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
            if (await catalog.TableExistsAsync(destinationConnection, transaction, table, cancellationToken))
            {
                SqlServerCopyProgress.ReportTable(request, table, $"Skipping existing schema {counter} of {tables.Count}");
                counter++;
                continue;
            }

            SqlServerCopyProgress.ReportTable(request, table, $"Copying schema {counter} of {tables.Count}");
            string script = await objectScripter.ScriptTableAsync(sourceConnectionString, table, cancellationToken);
            await SqlServerDatabaseCatalog.ExecuteNonQueryAsync(destinationConnection, transaction, script, cancellationToken);
            counter++;
        }
    }

    private async Task CopyScriptedObjectsAsync(
        CopyDatabaseRequest request,
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyCollection<SqlServerSchemaObject> objects,
        string objectType,
        CancellationToken cancellationToken,
        string? sqlObjectType = null)
    {
        int counter = 1;
        foreach (var item in objects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await catalog.ObjectExistsAsync(connection, transaction, item, cancellationToken, sqlObjectType))
            {
                SqlServerCopyProgress.Report(request, $"Skipping existing {objectType} {counter} of {objects.Count} ({item.QualifiedName})");
                counter++;
                continue;
            }

            SqlServerCopyProgress.Report(request, $"Copying {objectType} {counter} of {objects.Count} ({item.QualifiedName})");
            await SqlServerDatabaseCatalog.ExecuteNonQueryAsync(connection, transaction, item.Script, cancellationToken);
            counter++;
        }
    }
}

