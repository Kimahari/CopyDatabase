using CopyDatabase.Core.Requests;

using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.Core.Factories;

internal sealed class DatabaseCopyExecutorFactory : IDatabaseCopyExecutorFactory
{
    private readonly IServiceProvider serviceProvider;

    public DatabaseCopyExecutorFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IDatabaseCopyExecutor GetDatabaseCopyExecutor(DatabaseProvider provider)
    {
        return serviceProvider.GetRequiredKeyedService<IDatabaseCopyExecutor>(provider);
    }
}
