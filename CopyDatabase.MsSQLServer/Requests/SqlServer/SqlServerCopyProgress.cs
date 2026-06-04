using CopyDatabase.Core.Requests;

namespace CopyDatabase.MsSQLServer.Requests.SqlServer;

internal static class SqlServerCopyProgress
{
    public static void Report(CopyDatabaseRequest request, string message)
    {
        request.Progress?.Report(new DatabaseCopyProgress(request.DatabaseName, message));
    }

    public static void ReportTable(CopyDatabaseRequest request, SqlServerSchemaObject table, string message)
    {
        request.Progress?.Report(new DatabaseCopyProgress(request.DatabaseName, message, "Table", table.DisplayName));
    }
}

