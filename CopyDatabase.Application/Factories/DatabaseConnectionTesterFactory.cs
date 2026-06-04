using CopyDatabase.Core.Requests;

using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.Core.Factories;

internal sealed class DatabaseConnectionTesterFactory : IDatabaseConnectionTesterFactory
{
    private readonly IServiceProvider serviceProvider;

    public DatabaseConnectionTesterFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IDatabaseConnectionTester GetDatabaseConnectionTester(DatabaseProvider provider)
    {
        return serviceProvider.GetRequiredKeyedService<IDatabaseConnectionTester>(provider);
    }
}
