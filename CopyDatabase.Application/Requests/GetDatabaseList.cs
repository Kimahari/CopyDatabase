
namespace CopyDatabase.Core.Requests;

public sealed record GetDatabaseList : IRequest<string[]>
{
    public IDatabaseServerCredentials? ServerCredentials { get; set; }
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MsSQLServer;
}

public interface IDatabaseCatalogProvider
{
    Task<string[]> GetDatabaseNamesAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken);

    Task<string[]> GetSchemaObjectNamesAsync(
        IDatabaseServerCredentials credentials,
        string databaseName,
        SchemaObjectType objectType,
        CancellationToken cancellationToken);
}

public interface IDatabaseCatalogProviderFactory
{
    IDatabaseCatalogProvider GetDatabaseCatalogProvider(DatabaseProvider provider);
}

public sealed class GetDatabaseListCommandHandler : IRequestHandler<GetDatabaseList, string[]>
{
    private readonly IDatabaseCatalogProviderFactory catalogProviderFactory;

    public GetDatabaseListCommandHandler(IDatabaseCatalogProviderFactory catalogProviderFactory)
    {
        this.catalogProviderFactory = catalogProviderFactory;
    }

    public async Task<string[]> Handle(GetDatabaseList request, CancellationToken cancellationToken)
    {
        if (request.ServerCredentials is null) return Array.Empty<string>();

        var catalogProvider = catalogProviderFactory.GetDatabaseCatalogProvider(request.Provider);
        return await catalogProvider.GetDatabaseNamesAsync(request.ServerCredentials, cancellationToken);
    }
}


