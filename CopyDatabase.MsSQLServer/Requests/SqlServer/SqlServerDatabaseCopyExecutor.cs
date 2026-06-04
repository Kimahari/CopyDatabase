using CopyDatabase.Common;
using CopyDatabase.Core.Requests;

using Microsoft.Data.SqlClient;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal sealed class SqlServerDatabaseCopyExecutor : IDatabaseCopyExecutor
{
    private readonly IConnectionStringBuilder connectionStringBuilder;
    private readonly SqlServerDatabaseCatalog catalog;
    private readonly SqlServerSchemaCopier schemaCopier;
    private readonly SqlServerDataCopier dataCopier;

    public SqlServerDatabaseCopyExecutor()
        : this(new MsSQLConnectionStringBuilder(), new SqlServerDatabaseCatalog(), new SqlServerObjectScripter(), new SqlServerDataCopier())
    {
    }

    public SqlServerDatabaseCopyExecutor(
        IConnectionStringBuilder connectionStringBuilder,
        SqlServerDatabaseCatalog catalog,
        SqlServerObjectScripter objectScripter,
        SqlServerDataCopier dataCopier)
    {
        this.connectionStringBuilder = connectionStringBuilder;
        this.catalog = catalog;
        this.schemaCopier = new SqlServerSchemaCopier(catalog, objectScripter);
        this.dataCopier = dataCopier;
    }

    public async Task CopyAsync(CopyDatabaseRequest request, CancellationToken cancellationToken)
    {
        if (request.SourceCredentials is null) throw new InvalidOperationException("Source credentials are required.");
        if (request.DestinationCredentials is null) throw new InvalidOperationException("Destination credentials are required.");
        if (string.IsNullOrWhiteSpace(request.DatabaseName)) throw new InvalidOperationException("Database name is required.");

        string destinationDatabaseName = string.IsNullOrWhiteSpace(request.DestinationDatabaseName)
            ? request.DatabaseName
            : request.DestinationDatabaseName;

        string sourceConnection = BuildConnection(request.SourceCredentials, request.DatabaseName);
        string destinationServerConnection = BuildConnection(request.DestinationCredentials);
        string destinationDatabaseConnection = BuildConnection(request.DestinationCredentials, destinationDatabaseName);

        var tables = await catalog.LoadTablesAsync(sourceConnection, cancellationToken);
        var views = await catalog.LoadScriptedObjectsAsync(sourceConnection, SqlServerSql.Views, cancellationToken);
        var routines = await catalog.LoadScriptedObjectsAsync(sourceConnection, SqlServerSql.Routines, cancellationToken);

        if (request.DropDestinationDatabase)
        {
            SqlServerCopyProgress.Report(request, "Removing destination database if it exists");
            await catalog.RecreateDatabaseAsync(destinationServerConnection, destinationDatabaseName, cancellationToken);
        }
        else
        {
            SqlServerCopyProgress.Report(request, "Creating destination database if needed");
            await catalog.CreateDatabaseIfMissingAsync(destinationServerConnection, destinationDatabaseName, cancellationToken);
        }

        await using var destinationConnection = new SqlConnection(destinationDatabaseConnection);
        await destinationConnection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await destinationConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (request.CopySchema)
            {
                await schemaCopier.CopyAsync(request, sourceConnection, destinationConnection, transaction, tables, routines, views, cancellationToken);
            }

            if (request.CopyData)
            {
                await dataCopier.CopyAsync(request, sourceConnection, destinationConnection, transaction, tables, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            SqlServerCopyProgress.Report(request, "Copied");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private string BuildConnection(IDatabaseServerCredentials credentials, string databaseName = "")
    {
        return connectionStringBuilder
            .BuildConnection(credentials, databaseName)
            .FromSecureString();
    }
}

