using CopyDatabase.Core.Requests;

using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.Core.Factories;

internal sealed class DatabaseCatalogProviderFactory : IDatabaseCatalogProviderFactory
{
    private readonly IServiceProvider serviceProvider;

    public DatabaseCatalogProviderFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IDatabaseCatalogProvider GetDatabaseCatalogProvider(DatabaseProvider provider)
    {
        return serviceProvider.GetRequiredKeyedService<IDatabaseCatalogProvider>(provider);
    }
}
