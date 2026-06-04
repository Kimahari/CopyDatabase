namespace CopyDatabase.Core.Requests;

public enum SchemaObjectType
{
    Table,
    View,
    Routine
}

public sealed record GetSchemaObjectList : IRequest<string[]>
{
    public IDatabaseServerCredentials? ServerCredentials { get; set; }
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.MsSQLServer;
    public string DatabaseName { get; set; } = "";
    public SchemaObjectType ObjectType { get; set; }
}

public sealed class GetSchemaObjectListCommandHandler : IRequestHandler<GetSchemaObjectList, string[]>
{
    private readonly IDatabaseCatalogProviderFactory catalogProviderFactory;

    public GetSchemaObjectListCommandHandler(IDatabaseCatalogProviderFactory catalogProviderFactory)
    {
        this.catalogProviderFactory = catalogProviderFactory;
    }

    public async Task<string[]> Handle(GetSchemaObjectList request, CancellationToken cancellationToken)
    {
        if (request.ServerCredentials is null || string.IsNullOrWhiteSpace(request.DatabaseName)) return Array.Empty<string>();

        var catalogProvider = catalogProviderFactory.GetDatabaseCatalogProvider(request.Provider);
        return await catalogProvider.GetSchemaObjectNamesAsync(
            request.ServerCredentials,
            request.DatabaseName,
            request.ObjectType,
            cancellationToken);
    }
}
