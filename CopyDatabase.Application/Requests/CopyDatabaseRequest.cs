namespace CopyDatabase.Core.Requests;

public sealed record DatabaseCopyProgress(string DatabaseName, string Message, string? ObjectType = null, string? ObjectName = null);

public sealed record CopyDatabaseRequest : IRequest
{
    public IDatabaseServerCredentials? SourceCredentials { get; set; }
    public IDatabaseServerCredentials? DestinationCredentials { get; set; }
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MsSQLServer;
    public string DatabaseName { get; set; } = "";
    public string DestinationDatabaseName { get; set; } = "";
    public bool CopySchema { get; set; } = true;
    public bool CopyData { get; set; } = true;
    public bool DropDestinationDatabase { get; set; } = true;
    public IProgress<DatabaseCopyProgress>? Progress { get; set; }
}

public interface IDatabaseCopyExecutor
{
    Task CopyAsync(CopyDatabaseRequest request, CancellationToken cancellationToken);
}

public interface IDatabaseCopyExecutorFactory
{
    IDatabaseCopyExecutor GetDatabaseCopyExecutor(DatabaseProvider provider);
}

public sealed class CopyDatabaseCommandHandler : IRequestHandler<CopyDatabaseRequest>
{
    private readonly IDatabaseCopyExecutorFactory executorFactory;

    public CopyDatabaseCommandHandler(IDatabaseCopyExecutorFactory executorFactory)
    {
        this.executorFactory = executorFactory;
    }

    public Task Handle(CopyDatabaseRequest request, CancellationToken cancellationToken)
    {
        return HandleCore(request, cancellationToken);
    }

    private async Task HandleCore(CopyDatabaseRequest request, CancellationToken cancellationToken)
    {
        var executor = executorFactory.GetDatabaseCopyExecutor(request.Provider);
        await executor.CopyAsync(request, cancellationToken);
    }
}
